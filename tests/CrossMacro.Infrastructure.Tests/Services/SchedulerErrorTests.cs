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

public class SchedulerErrorTests
{
    private readonly IMacroFileManager _fileManager;
    private readonly Func<IMacroPlayer> _playerFactory;
    private readonly IMacroPlayer _player;
    private readonly ITimeProvider _timeProvider;
    private readonly SchedulerService _service;

    public SchedulerErrorTests()
    {
        _fileManager = Substitute.For<IMacroFileManager>();
        _player = Substitute.For<IMacroPlayer>();
        _playerFactory = () => _player;
        _timeProvider = Substitute.For<ITimeProvider>();
        _timeProvider.UtcNow.Returns(new DateTime(2024, 1, 1, 12, 0, 0));

        _service = new SchedulerService(_fileManager, _playerFactory, _timeProvider);
    }

    [Fact]
    public async Task RunTaskAsync_HandlesMissingMacroFile()
    {
        // Arrange
        var task = new ScheduledTask 
        { 
            Name = "Missing File Task", 
            MacroFilePath = "missing.json",
            IsEnabled = true 
        };
        _service.AddTask(task);

        _fileManager.LoadAsync("missing.json").Returns(Task.FromResult<MacroSequence?>(null));

        bool executed = false;
        string? errorMsg = null;
        _service.TaskExecuted += (s, e) => 
        {
            executed = true;
            errorMsg = e.Message;
        };

        // Act
        await _service.RunTaskAsync(task.Id);

        // Assert
        executed.Should().BeTrue();
        // Since File.Exists returns false (no real file), we expect "Macro file not found"
        errorMsg.Should().Contain("Macro file not found"); 
        task.LastStatus.Should().Contain("not found");
    }

    [Fact]
    public async Task RunTaskAsync_HandlesPlaybackException_Gracefully()
    {
        // Arrange
        var task = new ScheduledTask 
        { 
            Name = "Error Task", 
            MacroFilePath = Path.GetTempFileName(), // Real file so checking Exists passes
            IsEnabled = true 
        };
        _service.AddTask(task);

        var macro = new MacroSequence { Events = { new MacroEvent() } };
        _fileManager.LoadAsync(Arg.Any<string>()).Returns(macro); // Mock load regardless of path

        // Player throws unexpected exception
        _player.When(p => p.PlayAsync(Arg.Any<MacroSequence>(), Arg.Any<PlaybackOptions>()))
               .Do(x => throw new Exception("Unexpected crash"));

        bool success = true;
        _service.TaskExecuted += (s, e) => success = e.Success;

        try 
        {
            // Act
            await _service.RunTaskAsync(task.Id);
        }
        finally
        {
            if (File.Exists(task.MacroFilePath)) File.Delete(task.MacroFilePath);
        }

        // Assert
        success.Should().BeFalse();
        task.LastStatus.Should().Contain("Error");
    }
}
