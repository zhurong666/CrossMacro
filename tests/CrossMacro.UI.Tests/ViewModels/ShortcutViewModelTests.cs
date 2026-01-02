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

public class ShortcutViewModelTests
{
    private readonly IShortcutService _shortcutService;
    private readonly IDialogService _dialogService;
    private readonly ShortcutViewModel _viewModel;

    public ShortcutViewModelTests()
    {
        _shortcutService = Substitute.For<IShortcutService>();
        _dialogService = Substitute.For<IDialogService>();
        
        _shortcutService.Tasks.Returns(new ObservableCollection<ShortcutTask>());

        _viewModel = new ShortcutViewModel(_shortcutService, _dialogService);
    }

    [Fact]
    public async Task Construction_LoadsAndStartsService()
    {
        await _shortcutService.Received(1).LoadAsync();
        _shortcutService.Received(1).Start();
    }

    [Fact]
    public void AddTask_CreatesAndSelectsTask()
    {
        // Act
        _viewModel.AddTaskCommand.Execute(null);

        // Assert
        _shortcutService.Received(1).AddTask(Arg.Any<ShortcutTask>());
        _viewModel.SelectedTask.Should().NotBeNull();
        _viewModel.SelectedTask!.Name.Should().Contain("Shortcut");
    }

    [Fact]
    public async Task RemoveTask_WhenConfirmed_RemovesTask()
    {
        // Arrange
        var task = new ShortcutTask();
        _shortcutService.Tasks.Add(task);
        _dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        // Act
        await _viewModel.RemoveTaskCommand.ExecuteAsync(task);

        // Assert
        _shortcutService.Received(1).RemoveTask(task.Id);
        _ = _shortcutService.Received(1).SaveAsync();
    }

    [Fact]
    public async Task RemoveTask_WhenCancelled_DoesNotRemove()
    {
        // Arrange
        var task = new ShortcutTask();
        _shortcutService.Tasks.Add(task);
        _dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        // Act
        await _viewModel.RemoveTaskCommand.ExecuteAsync(task);

        // Assert
        _shortcutService.DidNotReceive().RemoveTask(Arg.Any<System.Guid>());
    }

    [Fact]
    public void OnHotkeyChanged_UpdatesSelectedTask()
    {
        // Arrange
        var task = new ShortcutTask();
        _viewModel.SelectedTask = task;

        // Act
        _viewModel.OnHotkeyChanged("F9");

        // Assert
        task.HotkeyString.Should().Be("F9");
        _viewModel.SelectedHotkeyString.Should().Be("F9");
    }
}
