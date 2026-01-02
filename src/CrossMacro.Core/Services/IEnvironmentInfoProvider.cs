namespace CrossMacro.Core.Services;

/// <summary>
/// Provides platform-agnostic environment information for UI decisions.
/// Implementations are platform-specific but the interface lives in Core.
/// </summary>
public interface IEnvironmentInfoProvider
{
    /// <summary>
    /// Gets the detected display environment.
    /// </summary>
    DisplayEnvironment CurrentEnvironment { get; }
    
    /// <summary>
    /// Whether the window manager handles its own close button.
    /// True for tiling WMs like Hyprland, i3, sway where custom title bars are not needed.
    /// </summary>
    bool WindowManagerHandlesCloseButton { get; }
}
