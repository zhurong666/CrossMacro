using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Notifications;
using CrossMacro.UI.ViewModels;

namespace CrossMacro.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize NotificationManager
        var notificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3
        };

        // Assign to ViewModel when DataContext changes
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.NotificationManager = notificationManager;
            }
        };
    }

    private void OnStartRecording(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StartRecording();
        }
    }

    private void OnStopRecording(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.StopRecording();
        }
    }

    private void OnPlayMacro(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PlayMacro();
        }
    }

    private void OnSaveMacro(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SaveMacro();
        }
    }

    private void OnLoadMacro(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.LoadMacro();
        }
    }

    private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnCloseApp(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
