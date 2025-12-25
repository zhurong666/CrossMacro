using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CrossMacro.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        
        // Initialize NotificationManager
        var notificationManager = new Avalonia.Controls.Notifications.WindowNotificationManager(this)
        {
            Position = Avalonia.Controls.Notifications.NotificationPosition.TopRight,
            MaxItems = 3
        };
        
        if (DataContext is ViewModels.MainWindowViewModel vm)
        {
            vm.NotificationManager = notificationManager;
        }
    }
    
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
    
    private void OnMinimizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void OnCloseApp(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
