using System;
using System.IO;
using CrossMacro.Core.Services;
using Serilog;

namespace CrossMacro.Platform.Linux.Services;

/// <summary>
/// Linux-specific implementation of permission checking.
/// Handles /dev/uinput access and accessibility settings.
/// </summary>
public class LinuxPermissionChecker : IPermissionChecker
{
    public bool IsSupported => true;

    public bool IsAccessibilityTrusted()
    {

        return CheckUInputAccess();
    }

    public bool CheckUInputAccess()
    {
        try
        {
            // Helper to check write access
            bool CheckWrite(string path)
            {
                if (!File.Exists(path)) return false;
                try
                {
                    using var fs = File.OpenWrite(path);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to check permission for {Path}", path);
                    return false;
                }
            }

            // Check standard paths
            if (CheckWrite(LinuxConstants.UInputDevicePath)) return true;
            if (CheckWrite(LinuxConstants.UInputAlternatePath)) return true;
            
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking uinput permissions");
            return false;
        }
    }

    public void OpenAccessibilitySettings()
    {

        try 
        {

             System.Diagnostics.Process.Start("xdg-open", "settings://");
        }
        catch(Exception ex)
        {
             Log.Error(ex, "Failed to open accessibility settings");
        }
    }
}
