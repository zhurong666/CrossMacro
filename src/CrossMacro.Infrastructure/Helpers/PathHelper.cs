using System;
using System.IO;
using CrossMacro.Core;

namespace CrossMacro.Infrastructure.Helpers;

/// <summary>
/// Helper for resolving application paths following XDG Base Directory specification
/// </summary>
public static class PathHelper
{
    public static string GetConfigDirectory()
    {
        // Check XDG_CONFIG_HOME first (Linux standard)
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        
        if (string.IsNullOrEmpty(xdgConfigHome))
        {
            // Fallback to platform-specific default:
            // Windows: %APPDATA% (Roaming)
            // Linux/macOS: ~/.config (usually)
            xdgConfigHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        return Path.Combine(xdgConfigHome, AppConstants.AppIdentifier);
    }

    public static string GetConfigFilePath(string fileName)
    {
        return Path.Combine(GetConfigDirectory(), fileName);
    }
}
