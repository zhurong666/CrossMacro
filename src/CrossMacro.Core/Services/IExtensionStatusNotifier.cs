using System;

namespace CrossMacro.Core.Services;

/// <summary>
/// Interface for position providers that may need external extensions
/// and want to notify UI about their status.
/// 
/// This allows UI to subscribe to extension status events without
/// depending on specific platform implementations.
/// </summary>
public interface IExtensionStatusNotifier
{
    /// <summary>
    /// Fired when extension status changes (e.g., GNOME extension enabled/disabled).
    /// Message contains status text for UI display.
    /// </summary>
    event EventHandler<string>? ExtensionStatusChanged;
}
