namespace CrossMacro.UI;

/// <summary>
/// Centralized UI strings for dialogs and messages.
/// </summary>
public static class UIStrings
{
    /// <summary>
    /// Permission dialog title.
    /// </summary>
    public const string PermissionRequiredTitle = "Permission Required";
    
    /// <summary>
    /// Permission dialog message for macOS accessibility.
    /// </summary>
    public const string MacOSAccessibilityMessage = 
        "CrossMacro requires Accessibility permissions to capture keyboard and mouse input.\n\n" +
        "Would you like to open System Settings now?";
    
    /// <summary>
    /// Open settings button text.
    /// </summary>
    public const string OpenSettingsButton = "Open Settings";
    
    /// <summary>
    /// Later button text.
    /// </summary>
    public const string LaterButton = "Later";
}
