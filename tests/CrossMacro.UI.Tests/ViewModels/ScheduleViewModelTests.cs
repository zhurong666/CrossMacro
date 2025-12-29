using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.UI.Services;
using CrossMacro.UI.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CrossMacro.UI.Tests.ViewModels;

public class ScheduleViewModelTests
{
    private readonly ISchedulerService _schedulerService;
    private readonly IDialogService _dialogService;
    private readonly ScheduleViewModel _viewModel;

    public ScheduleViewModelTests()
    {
        _schedulerService = Substitute.For<ISchedulerService>();
        _dialogService = Substitute.For<IDialogService>();
        
        // Setup initial tasks list
        _schedulerService.Tasks.Returns(new ObservableCollection<ScheduledTask>());

        _viewModel = new ScheduleViewModel(_schedulerService, _dialogService);
    }

    [Fact]
    public async Task Construction_LoadsAndStartsService()
    {
        await _schedulerService.Received(1).LoadAsync();
        _schedulerService.Received(1).Start();
    }

    [Fact]
    public void AddTask_CreatesAndSelectsTask()
    {
        // Act
        _viewModel.AddTaskCommand.Execute(null);

        // Assert
        _schedulerService.Received(1).AddTask(Arg.Any<ScheduledTask>());
        _viewModel.SelectedTask.Should().NotBeNull();
        _viewModel.SelectedTask!.Name.Should().Contain("Task");
    }

    [Fact]
    public async Task RemoveTask_WhenConfirmed_RemovesTask()
    {
        // Arrange
        var task = new ScheduledTask();
        _schedulerService.Tasks.Add(task);
        _dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        // Act
        await _viewModel.RemoveTaskCommand.ExecuteAsync(task);

        // Assert
        _schedulerService.Received(1).RemoveTask(task.Id);
        _schedulerService.Received(1).SaveAsync();
    }

    [Fact]
    public async Task RemoveTask_WhenCancelled_DoesNotRemove()
    {
        // Arrange
        var task = new ScheduledTask();
        _schedulerService.Tasks.Add(task);
        _dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        // Act
        await _viewModel.RemoveTaskCommand.ExecuteAsync(task);

        // Assert
        _schedulerService.DidNotReceive().RemoveTask(Arg.Any<System.Guid>());
    }

    [Fact]
    public void ScheduleTypeSelection_UpdatesTaskType()
    {
        // Arrange
        var task = new ScheduledTask { Type = ScheduleType.Interval };
        _viewModel.SelectedTask = task;

        // Act
        _viewModel.IsDateTimeSelected = true;

        // Assert
        task.Type.Should().Be(ScheduleType.SpecificTime);
        _viewModel.IsIntervalSelected.Should().BeFalse();

        // Act 2
        _viewModel.IsIntervalSelected = true;

        // Assert 2
        task.Type.Should().Be(ScheduleType.Interval);
        _viewModel.IsDateTimeSelected.Should().BeFalse();
    }

    [Fact]
    public async Task BrowseMacro_UpdatesTaskPath()
    {
        // Arrange
        var task = new ScheduledTask();
        _viewModel.SelectedTask = task;
        _dialogService.ShowOpenFileDialogAsync(Arg.Any<string>(), Arg.Any<FileDialogFilter[]>())
            .Returns(Task.FromResult<string?>("test.macro"));

        // Act
        await _viewModel.BrowseMacroCommand.ExecuteAsync(null);

        // Assert
        task.MacroFilePath.Should().Be("test.macro");
    }
}
