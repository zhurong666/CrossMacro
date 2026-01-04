namespace CrossMacro.Core.Models;

/// <summary>
/// Types of actions available in the macro editor.
/// </summary>
public enum EditorActionType
{
    /// <summary>
    /// Move mouse to coordinates (absolute or relative based on IsAbsolute flag).
    /// </summary>
    MouseMove,
    
    /// <summary>
    /// Click mouse button (press + release).
    /// </summary>
    MouseClick,
    
    /// <summary>
    /// Press and hold mouse button.
    /// </summary>
    MouseDown,
    
    /// <summary>
    /// Release mouse button.
    /// </summary>
    MouseUp,
    
    /// <summary>
    /// Press and release a keyboard key.
    /// </summary>
    KeyPress,
    
    /// <summary>
    /// Press and hold a keyboard key.
    /// </summary>
    KeyDown,
    
    /// <summary>
    /// Release a keyboard key.
    /// </summary>
    KeyUp,
    
    /// <summary>
    /// Wait for specified milliseconds.
    /// </summary>
    Delay,
    
    /// <summary>
    /// Scroll vertically (up/down).
    /// </summary>
    ScrollVertical,
    
    /// <summary>
    /// Scroll horizontally (left/right).
    /// </summary>
    ScrollHorizontal
}
