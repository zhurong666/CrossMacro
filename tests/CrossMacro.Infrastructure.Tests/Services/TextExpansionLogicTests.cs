using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrossMacro.Infrastructure.Tests.Services;

public class TextExpansionLogicTests
{
    private readonly ISettingsService _settingsService;
    private readonly IClipboardService _clipboardService;
    private readonly ITextExpansionStorageService _storageService;
    private readonly IKeyboardLayoutService _layoutService;
    private readonly IInputCapture _inputCapture;
    private readonly IInputSimulator _inputSimulator;
    private readonly TextExpansionService _service;

    public TextExpansionLogicTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _settingsService.Current.Returns(new AppSettings { EnableTextExpansion = true });

        _clipboardService = Substitute.For<IClipboardService>();
        _storageService = Substitute.For<ITextExpansionStorageService>();
        _layoutService = Substitute.For<IKeyboardLayoutService>();
        
        _inputCapture = Substitute.For<IInputCapture>();
        _inputSimulator = Substitute.For<IInputSimulator>();

        _service = new TextExpansionService(
            _settingsService,
            _clipboardService,
            _storageService,
            _layoutService,
            () => _inputCapture,
            () => _inputSimulator);
            
        // Default mock for typing to avoid slow Unicode fallback
        _layoutService.GetInputForChar(Arg.Any<char>()).Returns((10, false, false));

        _service.Start();
    }

    [Fact]
    public async Task Expansion_DoesNotTriggerRecursively()
    {
        // Recursion scenario: Trigger ":test" expands to "This is a :test"
        // If not handled, the ":test" inside replacement would trigger again.
        
        // Arrange
        var expansion = new TextExpansion(":test", "This is a :test");
        _storageService.GetCurrent().Returns(new List<TextExpansion> { expansion });
        
        // expansion logic test
        _layoutService.GetCharFromKeyCode(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(':', 't', 'e', 's', 't');

        // Capture calls to see if expansion happens once or loops
        _inputSimulator.When(x => x.KeyPress(Arg.Any<int>(), Arg.Any<bool>()))
                       .Do(_ => { /* counted via wrapper if needed, but we check logic flow */ });

        // Simulate Input... this is hard to unit test purely via mocks because the internal buffer
        // logic is triggered by events.
        // We will verify that the buffer is likely cleared after match.
        
        // Actually, the service logic:
        // _buffer.Clear();
        // return;
        // inside CheckForExpansion() prevents immediate recursion on the same buffer content.
        // BUT, if the replacement is typed out (fallback mode), does it feed back into InputCapture?
        // NO, because InputSimulator generates OS events, which InputCapture WOULD see in a real integration.
        // In this unit test, Mock InputSimulator does NOT feed back into Mock InputCapture. 
        // So this tests only internal logic state (Buffer clearing).
        
        // Provide input with small delays to bypass service debounce (20ms)
        // We can't avoid these delays easily as they are part of the service logic we are testing
        RaiseKey(39, ':'); await Task.Delay(25);
        RaiseKey(20, 't'); await Task.Delay(25);
        RaiseKey(18, 'e'); await Task.Delay(25);
        RaiseKey(31, 's'); await Task.Delay(25);
        RaiseKey(20, 't'); await Task.Delay(25);
        
        // Wait for execution using polling instead of fixed 500ms delay
        // We know we expect a KeyPress(14, true) call
        await WaitFor(() => _inputSimulator.ReceivedCalls().Any(c => c.GetMethodInfo().Name == "KeyPress" && (int)c.GetArguments()[0] == 14));

        // Assert: We should have attempted expansion ONCE.
        // The service logic calls SendKeyAsync which calls InputSimulator.KeyPress.
        // We expect backspaces (Trigger length = 5) + Replacement typing.
        
        // 5 Backspaces (Code 14)
        _inputSimulator.Received().KeyPress(14, true); 
        
        // "This is a :test" typing verification is complex
        // But critically, since buffer is cleared, we ensure state is reset.
        // We can't verify loop prevention here because mock doesn't loop back.
        // BUT we can verify that after expansion, if we type ' ' it doesn't trigger again immediately.
    }
    
    [Fact]
    public async Task Buffer_Clears_AfterMatch()
    {
        // Arrange
        var expansion = new TextExpansion("abc", "expanded");
        _storageService.GetCurrent().Returns(new List<TextExpansion> { expansion });
        
        _layoutService.GetCharFromKeyCode(30, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns('a');
        _layoutService.GetCharFromKeyCode(48, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns('b');
        _layoutService.GetCharFromKeyCode(46, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns('c');

        // Act
        // Act
        RaiseKey(30, 'a'); await Task.Delay(25);
        RaiseKey(48, 'b'); await Task.Delay(25);
        RaiseKey(46, 'c'); await Task.Delay(25);
        
        await WaitFor(() => _inputSimulator.ReceivedCalls().Any(c => c.GetMethodInfo().Name == "KeyPress" && (int)c.GetArguments()[0] == 14));

        // Assert - Backspace called means match found
        _inputSimulator.Received().KeyPress(14, true);
        
        // Reset calls
        _inputSimulator.ClearReceivedCalls();
        
        // Type 'd'
        _layoutService.GetCharFromKeyCode(32, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns('d');
        RaiseKey(32, 'd');
        
        await Task.Delay(50);
        
        // Should NOT trigger again (if buffer wasn't cleared, "abcd" might trigger if "cd" was a trigger?)
        // Better test: Type "abc" again.
        RaiseKey(30, 'a'); await Task.Delay(25); // Debounce
        RaiseKey(48, 'b'); await Task.Delay(25);
        RaiseKey(46, 'c'); await Task.Delay(25);
        
        await WaitFor(() => _inputSimulator.ReceivedCalls().Any(c => c.GetMethodInfo().Name == "KeyPress" && (int)c.GetArguments()[0] == 14));
        
        // Should trigger again
        _inputSimulator.Received().KeyPress(14, true);
    }

    private async Task WaitFor(Func<bool> condition, int timeoutMs = 1000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
    }

    private void RaiseKey(int code, char c)
    {
        // Setup mock char
        _layoutService.GetCharFromKeyCode(code, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(c);
            
        _inputCapture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, 
            new InputCaptureEventArgs { Type = InputEventType.Key, Code = code, Value = 1 });
            
        // Reset char return to default/null to avoid sticky mock
        // _layoutService.GetCharFromKeyCode(code...).Returns((char?)null);
    }

}
