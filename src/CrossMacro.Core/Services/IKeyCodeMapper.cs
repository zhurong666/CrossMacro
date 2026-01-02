namespace CrossMacro.Core.Services;

/// <summary>
/// Maps between key names and key codes.
/// Provides platform-agnostic key code resolution with keyboard layout support.
/// </summary>
public interface IKeyCodeMapper
{
    /// <summary>
    /// Gets the key code for a given key name.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Ctrl", "F1", "Mouse Left")</param>
    /// <returns>The key code, or -1 if not found</returns>
    int GetKeyCode(string keyName);
    
    /// <summary>
    /// Gets the key name for a given key code.
    /// </summary>
    /// <param name="keyCode">The key code</param>
    /// <returns>The human-readable key name</returns>
    string GetKeyName(int keyCode);
    
    /// <summary>
    /// Determines if the given key code is a modifier key (Ctrl, Shift, Alt, etc.)
    /// </summary>
    /// <param name="code">The key code to check</param>
    /// <returns>True if the key is a modifier</returns>
    bool IsModifierKeyCode(int code);
}
