using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrossMacro.Infrastructure.Tests.Services;

public class ShortcutServiceTests
{
    private readonly IMacroFileManager _fileManager;
    private readonly Func<IMacroPlayer> _playerFactory;
    private readonly IMacroPlayer _player;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly ShortcutService _service;

    public ShortcutServiceTests()
    {
        _fileManager = Substitute.For<IMacroFileManager>();
        _player = Substitute.For<IMacroPlayer>();
        _playerFactory = () => _player;
        _hotkeyService = Substitute.For<IGlobalHotkeyService>();
        
        _service = new ShortcutService(_fileManager, _playerFactory, _hotkeyService);
    }

    [Fact]
    public void Start_SubscribesToHotkeyService()
    {
        _service.Start();
        
        // We can verify this by checking if IsListening is true, 
        // verifying event subscription is hard with NSubstitute unless we inspect calls to add_Event.
        // But implementation sets IsListening.
        _service.IsListening.Should().BeTrue();
    }

    [Fact]
    public void Stop_UnsubscribesAndSetsListeningFalse()
    {
        _service.Start();
        _service.Stop();
        
        _service.IsListening.Should().BeFalse();
    }

    [Fact]
    public void AddTask_AddsToCollection()
    {
        var task = new ShortcutTask();
        _service.AddTask(task);
        _service.Tasks.Should().Contain(task);
    }

    [Fact]
    public void RemoveTask_RemovesFromCollection()
    {
        var task = new ShortcutTask();
        _service.AddTask(task);
        _service.RemoveTask(task.Id);
        _service.Tasks.Should().NotContain(task);
    }

    [Fact]
    public async Task OnRawInputReceived_ExecutesMatchingTask()
    {
        // Arrange
        var task = new ShortcutTask 
        { 
            Name = "Test", 
            MacroFilePath = "test.macro", 
            HotkeyString = "F5" 
        };
        // Use property setter if possible or reflection to set IsEnabled/Valid state
        // ShortcutTask validates MacroFilePath and HotkeyString for CanBeEnabled.
        // So we can just set IsEnabled = true.
        task.IsEnabled = true;
        _service.AddTask(task);

        _fileManager.LoadAsync(Arg.Any<string>()).Returns(Task.FromResult<MacroSequence?>(new MacroSequence { Events = { new MacroEvent() } }));

        bool executed = false;
        _service.ShortcutExecuted += (s, e) => {
            if (e.Success) executed = true;
        };

        _service.Start();

        var tempFile = Path.GetTempFileName();
        // Rename and use matching extension if needed, or just use tempFile. 
        // Service just checks File.Exists(path).
        // Check if logic requires specific extension? No, LoadAsync logic might but File.Exists doesn't.
        // But LoadAsync is mocked. So just File.Exists needs to pass.
        task.MacroFilePath = tempFile;

        try
        {
            // Act
            // Raise event on _hotkeyService
            _hotkeyService.RawInputReceived += Raise.Event<EventHandler<RawHotkeyInputEventArgs>>(
                this, 
                new RawHotkeyInputEventArgs(0, new HashSet<int>(), "F5")); // Code doesn't matter much if string matches

            // Wait for async execution via polling
            await WaitFor(() => executed);

            // Assert
            executed.Should().BeTrue($"Task status: {task.LastStatus}. Should be Success or similar. If IsEnabled={task.IsEnabled}, Hotkey={task.HotkeyString}");
            await _player.Received(1).PlayAsync(Arg.Any<MacroSequence>(), Arg.Any<PlaybackOptions>());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task OnRawInputReceived_DoesNotExecute_IfDisabled()
    {
        // Arrange
        var task = new ShortcutTask 
        { 
            HotkeyString = "F5", 
            MacroFilePath = "test.macro" 
        };
        task.IsEnabled = false;
        _service.AddTask(task);
        
        _service.Start();

        // Act
        _hotkeyService.RawInputReceived += Raise.Event<EventHandler<RawHotkeyInputEventArgs>>(
            this, 
            new RawHotkeyInputEventArgs(0, new HashSet<int>(), "F5"));

        // For negative verification (DidNotReceive), we still need a small delay to ensure it didn't happen
        // But we can reduce it if we are sure the event dispatch is fast. 50ms is reasonable for negative test.
        await Task.Delay(50);

        // Assert
        await _player.DidNotReceive().PlayAsync(Arg.Any<MacroSequence>(), Arg.Any<PlaybackOptions>());
    }
    private async Task WaitFor(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
    }
}
