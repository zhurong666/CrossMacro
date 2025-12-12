using Avalonia.Controls;
using Avalonia.Threading;
using CrossMacro.UI.Controls;
using CrossMacro.UI.ViewModels;
using System;
using System.Threading.Tasks;

namespace CrossMacro.UI.Views.Tabs;

public partial class SettingsTabView : UserControl
{
    private HotkeyCapture? _recordingHotkeyCapture;
    private HotkeyCapture? _playbackHotkeyCapture;
    private HotkeyCapture? _pauseHotkeyCapture;
    private Border? _toastNotification;
    private TextBlock? _toastMessage;
    
    public SettingsTabView()
    {
        InitializeComponent();
        
        // Wire up validation after the controls are loaded
        this.Loaded += OnLoaded;
    }
    
    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Get references to the HotkeyCapture controls
        _recordingHotkeyCapture = this.FindControl<HotkeyCapture>("RecordingHotkeyCapture");
        _playbackHotkeyCapture = this.FindControl<HotkeyCapture>("PlaybackHotkeyCapture");
        _pauseHotkeyCapture = this.FindControl<HotkeyCapture>("PauseHotkeyCapture");
        
        // Get references to toast notification elements
        _toastNotification = this.FindControl<Border>("ToastNotification");
        _toastMessage = this.FindControl<TextBlock>("ToastMessage");
        
        var viewModel = DataContext as SettingsViewModel;
        
        if (_recordingHotkeyCapture != null && viewModel != null)
        {
            _recordingHotkeyCapture.ValidationFunc = (newHotkey) =>
            {
                if (newHotkey == viewModel.PlaybackHotkey)
                {
                    ShowToast("This hotkey is already assigned to Playback");
                    return (false, "This hotkey is already assigned to Playback");
                }
                if (newHotkey == viewModel.PauseHotkey)
                {
                    ShowToast("This hotkey is already assigned to Pause");
                    return (false, "This hotkey is already assigned to Pause");
                }
                return (true, string.Empty);
            };
        }
        
        if (_playbackHotkeyCapture != null && viewModel != null)
        {
            _playbackHotkeyCapture.ValidationFunc = (newHotkey) =>
            {
                if (newHotkey == viewModel.RecordingHotkey)
                {
                    ShowToast("This hotkey is already assigned to Recording");
                    return (false, "This hotkey is already assigned to Recording");
                }
                if (newHotkey == viewModel.PauseHotkey)
                {
                    ShowToast("This hotkey is already assigned to Pause");
                    return (false, "This hotkey is already assigned to Pause");
                }
                return (true, string.Empty);
            };
        }
        
        if (_pauseHotkeyCapture != null && viewModel != null)
        {
            _pauseHotkeyCapture.ValidationFunc = (newHotkey) =>
            {
                if (newHotkey == viewModel.RecordingHotkey)
                {
                    ShowToast("This hotkey is already assigned to Recording");
                    return (false, "This hotkey is already assigned to Recording");
                }
                if (newHotkey == viewModel.PlaybackHotkey)
                {
                    ShowToast("This hotkey is already assigned to Playback");
                    return (false, "This hotkey is already assigned to Playback");
                }
                return (true, string.Empty);
            };
        }
    }
    
    private void ShowToast(string message)
    {
        if (_toastNotification == null || _toastMessage == null)
            return;
            
        _toastMessage.Text = message;
        _toastNotification.IsVisible = true;
        _toastNotification.Opacity = 1.0;
        
        // Hide after 2 seconds
        Task.Delay(2000).ContinueWith(_ =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_toastNotification != null)
                {
                    _toastNotification.Opacity = 0.0;
                    
                    // Wait for fade animation to complete before hiding
                    Task.Delay(300).ContinueWith(__ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (_toastNotification != null)
                            {
                                _toastNotification.IsVisible = false;
                            }
                        });
                    });
                }
            });
        });
    }
}
