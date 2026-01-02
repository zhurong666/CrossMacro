using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.DisplayServer;

namespace CrossMacro.Platform.Linux.Services;

/// <summary>
/// Linux-specific implementation of IEnvironmentInfoProvider.
/// Wraps CompositorDetector for cross-platform abstraction.
/// </summary>
public class LinuxEnvironmentInfoProvider : IEnvironmentInfoProvider
{
    private readonly CompositorType _compositor;
    
    public LinuxEnvironmentInfoProvider()
    {
        _compositor = CompositorDetector.DetectCompositor();
    }
    
    /// <summary>
    /// Constructor for testing with explicit compositor type.
    /// </summary>
    internal LinuxEnvironmentInfoProvider(CompositorType compositor)
    {
        _compositor = compositor;
    }
    
    public DisplayEnvironment CurrentEnvironment => _compositor switch
    {
        CompositorType.X11 => DisplayEnvironment.LinuxX11,
        CompositorType.HYPRLAND => DisplayEnvironment.LinuxHyprland,
        CompositorType.KDE => DisplayEnvironment.LinuxKDE,
        CompositorType.GNOME => DisplayEnvironment.LinuxGnome,
        CompositorType.Other => DisplayEnvironment.LinuxWayland,
        _ => DisplayEnvironment.Unknown
    };
    
    public bool WindowManagerHandlesCloseButton => 
        _compositor == CompositorType.HYPRLAND;
        // Future: Add detection for i3, sway, and other tiling WMs
}
