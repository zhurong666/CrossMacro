using CrossMacro.Core;

namespace CrossMacro.Platform.Linux;

/// <summary>
/// Linux platform-specific constants.
/// Centralizes device paths and platform magic numbers.
/// </summary>
public static class LinuxConstants
{
    /// <summary>
    /// Primary uinput device path.
    /// </summary>
    public const string UInputDevicePath = "/dev/uinput";
    
    /// <summary>
    /// Alternate uinput device path (some systems use this).
    /// </summary>
    public const string UInputAlternatePath = "/dev/input/uinput";
    
    /// <summary>
    /// X11 keycodes are offset from Linux keycodes by this value.
    /// X11 keycode = Linux keycode + 8
    /// </summary>
    public const int X11ToLinuxKeycodeOffset = 8;
}
