using System.Threading.Tasks;
using Tmds.DBus;

namespace CrossMacro.Platform.Linux.DisplayServer.Wayland;

/// <summary>
/// KDE Plasma DBus interface for keyboard layout detection.
/// </summary>
[DBusInterface("org.kde.KeyboardLayouts")]
public interface IKdeKeyboard : IDBusObject
{
    /// <summary>Returns the index of the currently active layout.</summary>
    Task<uint> getLayoutAsync();
    
    /// <summary>Returns list of layouts as (shortName, variant, displayName) tuples.</summary>
    Task<(string shortName, string variant, string displayName)[]> getLayoutsListAsync();
}
