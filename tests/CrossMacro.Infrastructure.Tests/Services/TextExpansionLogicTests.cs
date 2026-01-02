using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Core.Services.TextExpansion;
using CrossMacro.Infrastructure.Services;
using CrossMacro.Infrastructure.Services.TextExpansion;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrossMacro.Infrastructure.Tests.Services;

public class TextExpansionLogicTests
{
    private readonly ISettingsService _settingsService;
    private readonly ITextExpansionStorageService _storageService;
    private readonly IKeyboardLayoutService _layoutService;
    private readonly IInputCapture _inputCapture;
    
    // Components (Real or Mocked as needed)
    private readonly InputProcessor _inputProcessor;
    private readonly TextBufferState _bufferState;
    private readonly ITextExpansionExecutor _executor;
    
    private readonly TextExpansionService _service;

    public TextExpansionLogicTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _settingsService.Current.Returns(new AppSettings { EnableTextExpansion = true });

        _storageService = Substitute.For<ITextExpansionStorageService>();
        _layoutService = Substitute.For<IKeyboardLayoutService>();
        _inputCapture = Substitute.For<IInputCapture>();
        
        // Use Real Logic Components to test the flow
        _inputProcessor = new InputProcessor(_layoutService);
        _bufferState = new TextBufferState();
        _executor = Substitute.For<ITextExpansionExecutor>();

        _service = new TextExpansionService(
            _settingsService,
            _storageService,
            () => _inputCapture,
            _inputProcessor,
            _bufferState,
            _executor);
            
        // Default mock for typing
        _layoutService.GetCharFromKeyCode(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns((char?)null); // Default null unless specified

        _service.Start();
    }

    [Fact]
    public async Task ExpansionTriggered_WhenBufferMatches()
    {
        // Arrange
        var expansion = new Core.Models.TextExpansion("abc", "expanded");
        _storageService.GetCurrent().Returns(new List<Core.Models.TextExpansion> { expansion });
        
        SetupKey(30, 'a');
        SetupKey(48, 'b');
        SetupKey(46, 'c');

        // Act
        RaiseKey(30); await Task.Delay(25);
        RaiseKey(48); await Task.Delay(25);
        RaiseKey(46); await Task.Delay(25);
        
        // Wait for async execution
        await Task.Delay(100);

        // Assert
        // Executor should be called
        await _executor.Received(1).ExpandAsync(expansion);
    }
    
    [Fact]
    public async Task Buffer_Clears_AfterMatch()
    {
        // Arrange
        var expansion = new Core.Models.TextExpansion("abc", "expanded");
        _storageService.GetCurrent().Returns(new List<Core.Models.TextExpansion> { expansion });
        
        SetupKey(30, 'a');
        SetupKey(48, 'b');
        SetupKey(46, 'c');
        SetupKey(32, 'd');

        // Act - Trigger once
        RaiseKey(30); await Task.Delay(25);
        RaiseKey(48); await Task.Delay(25);
        RaiseKey(46); await Task.Delay(25);
        
        await Task.Delay(50);
        await _executor.Received(1).ExpandAsync(expansion);
        _executor.ClearReceivedCalls();

        // Act - Continue typing 'd'
        RaiseKey(32); await Task.Delay(25);
        
        // Ensure that typing 'abc' again triggers it again (proving buffer was reset, not stale)
        // If buffer wasn't cleared, it would be "abcd..."
        
        RaiseKey(30); await Task.Delay(25);
        RaiseKey(48); await Task.Delay(25);
        RaiseKey(46); await Task.Delay(25);
        
        await Task.Delay(50);
        
        // Assert - Should trigger again
        await _executor.Received(1).ExpandAsync(expansion);
    }

    private void SetupKey(int code, char c)
    {
        _layoutService.GetCharFromKeyCode(code, Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(c);
    }

    private void RaiseKey(int code)
    {
        _inputCapture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, 
            new InputCaptureEventArgs { Type = InputEventType.Key, Code = code, Value = 1 }); // Press
            
        // Simulate Release too for completeness? Not strictly needed for logic unless modifiers involved
        // _inputCapture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
        //     this, 
        //     new InputCaptureEventArgs { Type = InputEventType.Key, Code = code, Value = 0 }); 
    }
}
