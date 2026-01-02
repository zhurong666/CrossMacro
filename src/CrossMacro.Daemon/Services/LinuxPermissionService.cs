using System;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;
using CrossMacro.Platform.Linux.Native;

namespace CrossMacro.Daemon.Services;

public class LinuxPermissionService : ILinuxPermissionService
{
    [DllImport("libc", SetLastError = true)]
    private static extern int chown(string path, int owner, int group);

    public void ConfigureSocketPermissions(string socketPath)
    {
        try
        {
            // Try to find 'crossmacro' group GID
            int targetGid = -1;
            try 
            {
                if (File.Exists(LinuxSystemPaths.GroupFile))
                {
                    foreach (var line in File.ReadLines(LinuxSystemPaths.GroupFile))
                    {
                        if (line.StartsWith("crossmacro:"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length >= 3 && int.TryParse(parts[2], out int gid))
                            {
                                targetGid = gid;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to lookup crossmacro group GID: {Msg}", ex.Message);
            }

            if (targetGid != -1)
            {
                // -1 means don't change owner
                if (chown(socketPath, -1, targetGid) == 0)
                {
                    Log.Information("Set socket group to 'crossmacro' (GID: {Gid})", targetGid);
                }
                else
                {
                    Log.Warning("Failed to chown socket to crossmacro group. Errno: {Err}", Marshal.GetLastWin32Error());
                }
            }

            // Restricted: RW for User and Group (660)
            try 
            {
                if (OperatingSystem.IsLinux())
                {
                   SetUnixSocketPermissions(socketPath);
                }
            }
            catch (Exception ex)
            {
                 Log.Warning("Failed to set file mode: {Msg}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to set socket permissions: {Msg}", ex.Message);
        }
    }
    
    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    private static void SetUnixSocketPermissions(string socketPath)
    {
        File.SetUnixFileMode(socketPath, 
            UnixFileMode.UserRead | UnixFileMode.UserWrite | 
            UnixFileMode.GroupRead | UnixFileMode.GroupWrite);
        Log.Information("Socket permissions set to 660 (User+Group RW)");
    }
}
