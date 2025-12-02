using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossMacro.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CrossMacro.UI.Controls;

public partial class HotkeyCapture : UserControl
{
    public static readonly StyledProperty<string> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyCapture, string>(nameof(Hotkey), "F8");

    public static readonly DirectProperty<HotkeyCapture, bool> IsCapturingProperty =
        AvaloniaProperty.RegisterDirect<HotkeyCapture, bool>(
            nameof(IsCapturing),
            o => o.IsCapturing);

    private bool _isCapturing;

    public string Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        private set => SetAndRaise(IsCapturingProperty, ref _isCapturing, value);
    }

    public event EventHandler<string>? HotkeyChanged;

    public static readonly DirectProperty<HotkeyCapture, string> DisplayStringProperty =
        AvaloniaProperty.RegisterDirect<HotkeyCapture, string>(
            nameof(DisplayString),
            o => o.DisplayString);

    private string _displayString = "F8";

    public string DisplayString
    {
        get => _displayString;
        private set => SetAndRaise(DisplayStringProperty, ref _displayString, value);
    }

    public HotkeyCapture()
    {
        InitializeComponent();
        UpdateDisplayString();
        
        // Add hover effects manually
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }
    
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ApplyHoverEffect();
    }
    
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        // Don't remove hover effect if we're capturing a key
        if (!IsCapturing)
        {
            RemoveHoverEffect();
        }
    }
    
    private void ApplyHoverEffect()
    {
        var border = this.FindControl<Border>("HotkeyBorder");
        var icon = this.FindControl<Path>("EditIcon");
        
        if (border != null)
        {
            border.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#475569"));
            border.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#64748B"));
            border.BoxShadow = Avalonia.Media.BoxShadows.Parse("0 0 0 2 #1A94A3B8");
        }
        
        if (icon != null)
        {
            icon.Opacity = 1.0;
        }
    }
    
    private void RemoveHoverEffect()
    {
        var border = this.FindControl<Border>("HotkeyBorder");
        var icon = this.FindControl<Path>("EditIcon");
        
        if (border != null)
        {
            border.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#334155"));
            border.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("Transparent"));
            border.BoxShadow = default;
        }
        
        if (icon != null)
        {
            icon.Opacity = 0.5;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HotkeyProperty)
        {
            UpdateDisplayString();
        }
    }

    private void UpdateDisplayString()
    {
        DisplayString = IsCapturing ? "Press a key..." : Hotkey;
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        await StartCaptureAsync();
    }

    private async Task StartCaptureAsync()
    {
        if (IsCapturing) return;

        // Resolve service
        var app = Application.Current as App;
        var serviceProvider = typeof(App).GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(app) as IServiceProvider;
        var hotkeyService = serviceProvider?.GetService<IGlobalHotkeyService>();

        if (hotkeyService == null)
        {
            DisplayString = "Service Error";
            return;
        }

        IsCapturing = true;
        UpdateDisplayString();
        ApplyHoverEffect(); // Keep hover effect during capture

        try
        {
            // Capture directly from the service (bypassing UI/OS filtering)
            var newHotkey = await hotkeyService.CaptureNextKeyAsync();
            
            // Update on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                Hotkey = newHotkey;
                HotkeyChanged?.Invoke(this, newHotkey);
                IsCapturing = false;
                UpdateDisplayString();
                
                // Check if pointer is still over the control
                if (!IsPointerOver)
                {
                    RemoveHoverEffect();
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsCapturing = false;
                UpdateDisplayString();
                
                // Check if pointer is still over the control
                if (!IsPointerOver)
                {
                    RemoveHoverEffect();
                }
                
                Console.WriteLine($"Capture failed: {ex}");
            });
        }
    }
}
