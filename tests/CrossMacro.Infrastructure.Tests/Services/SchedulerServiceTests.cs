using System;
using System.Linq;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrossMacro.Infrastructure.Tests.Services;

public class SchedulerServiceTests
{
    private readonly IScheduledTaskRepository _repository;
    private readonly IScheduledTaskExecutor _executor;
    private readonly ITimeProvider _timeProvider;
    private readonly SchedulerService _service;

    public SchedulerServiceTests()
    {
        _repository = Substitute.For<IScheduledTaskRepository>();
        _executor = Substitute.For<IScheduledTaskExecutor>();
        _timeProvider = Substitute.For<ITimeProvider>();
        
        // Default time
        _timeProvider.UtcNow.Returns(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _service = new SchedulerService(_repository, _executor, _timeProvider);
    }

    [Fact]
    public void Start_SetsIsRunningToTrue()
    {
        _service.Start();
        _service.IsRunning.Should().BeTrue();
        _service.Stop();
    }

    [Fact]
    public void Stop_SetsIsRunningToFalse()
    {
        _service.Start();
        _service.Stop();
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void AddTask_AddsToCollection()
    {
        var task = new ScheduledTask();
        _service.AddTask(task);
        _service.Tasks.Should().Contain(task);
    }

    [Fact]
    public void RemoveTask_RemovesFromCollection()
    {
        var task = new ScheduledTask();
        _service.AddTask(task);
        _service.RemoveTask(task.Id);
        _service.Tasks.Should().NotContain(task);
    }

    [Fact]
    public void SetTaskEnabled_WhenTrue_CalculatesNextRunTime()
    {
        // Arrange
        var task = new ScheduledTask { MacroFilePath = "test.macro", Type = ScheduleType.Interval, IntervalValue = 60, IntervalUnit = IntervalUnit.Seconds };
        _service.AddTask(task);

        // Act
        _service.SetTaskEnabled(task.Id, true);

        // Assert
        var t = _service.Tasks.First(x => x.Id == task.Id);
        t.IsEnabled.Should().BeTrue();
        t.NextRunTime.Should().Be(_timeProvider.UtcNow.AddSeconds(60));
    }

    [Fact]
    public void SetTaskEnabled_WhenFalse_ClearsNextRunTime()
    {
        // Arrange
        var task = new ScheduledTask { MacroFilePath = "test.macro", IsEnabled = true };
        _service.AddTask(task); // Adds and calculates
        
        // Act
        _service.SetTaskEnabled(task.Id, false);

        // Assert
        var t = _service.Tasks.First(x => x.Id == task.Id);
        t.IsEnabled.Should().BeFalse();
        t.NextRunTime.Should().BeNull();
    }

    [Fact]
    public void UpdateTask_UpdatesExistingTaskProperties()
    {
        // Arrange
        var original = new ScheduledTask { Name = "Old Name", MacroFilePath = "old.macro" };
        _service.AddTask(original);

        var updated = new ScheduledTask 
        { 
            Id = original.Id, 
            Name = "New Name", 
            MacroFilePath = "new.macro" 
        };

        // Act
        _service.UpdateTask(updated);

        // Assert
        var t = _service.Tasks.First(x => x.Id == original.Id);
        t.Name.Should().Be("New Name");
        t.MacroFilePath.Should().Be("new.macro");
    }
}
