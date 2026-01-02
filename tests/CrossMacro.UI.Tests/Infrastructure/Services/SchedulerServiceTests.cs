using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace CrossMacro.UI.Tests.Infrastructure.Services;

public class SchedulerServiceTests
{
    private readonly IScheduledTaskRepository _repository;
    private readonly IScheduledTaskExecutor _executor;
    private readonly ITimeProvider _timeProvider;
    private readonly SchedulerService _sut; // System Under Test

    public SchedulerServiceTests()
    {
        _repository = Substitute.For<IScheduledTaskRepository>();
        _executor = Substitute.For<IScheduledTaskExecutor>();
        _timeProvider = Substitute.For<ITimeProvider>();
        
        // Default time
        _timeProvider.UtcNow.Returns(new DateTime(2025, 1, 1, 12, 0, 0));

        _sut = new SchedulerService(_repository, _executor, _timeProvider);
    }

    [Fact]
    public void AddTask_AddsTaskToCollection()
    {
        // Arrange
        var task = new ScheduledTask { Name = "Test Task" };

        // Act
        _sut.AddTask(task);

        // Assert
        Assert.Contains(task, _sut.Tasks);
    }

    [Fact]
    public void AddTask_CalculatesNextRunTime_IfEnabled()
    {
        // Arrange
        var task = new ScheduledTask 
        { 
            Name = "Test Task",
            MacroFilePath = "test.json", // Required for IsEnabled to be set to true
            IsEnabled = true,
            Type = ScheduleType.Interval,
            IntervalValue = 10,
            IntervalUnit = IntervalUnit.Seconds
        };
        // Ensure LastRunTime is set so it thinks it needs to run next
        task.LastRunTime = _timeProvider.UtcNow; 

        // Act
        _sut.AddTask(task);

        // Assert
        Assert.NotNull(task.NextRunTime);
    }

    [Fact]
    public void RemoveTask_RemovesTaskFromCollection()
    {
        // Arrange
        var task = new ScheduledTask { Name = "Test Task" };
        _sut.AddTask(task);

        // Act
        _sut.RemoveTask(task.Id);

        // Assert
        Assert.DoesNotContain(task, _sut.Tasks);
    }
}
