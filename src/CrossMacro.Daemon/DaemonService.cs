using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Ipc;
using CrossMacro.Daemon.Services;
using CrossMacro.Platform.Linux.Native;
using CrossMacro.Platform.Linux.Native.Systemd;
using Serilog;

namespace CrossMacro.Daemon;

public class DaemonService
{


    private Socket? _socket;
    
    private readonly ISecurityService _security;
    private readonly IVirtualDeviceManager _virtualDevice;
    private readonly IInputCaptureManager _inputCapture;
    private readonly ILinuxPermissionService _permissionService;

    public DaemonService(
        ISecurityService security, 
        IVirtualDeviceManager virtualDevice, 
        IInputCaptureManager inputCapture,
        ILinuxPermissionService permissionService)
    {
        _security = security;
        _virtualDevice = virtualDevice;
        _inputCapture = inputCapture;
        _permissionService = permissionService;
    }

    public async Task RunAsync(CancellationToken token)
    {
        // Try primary systemd-managed path first
        var socketPath = IpcProtocol.DefaultSocketPath;
        var socketDir = Path.GetDirectoryName(socketPath);
        
        // Check if we can use the primary path
        bool usePrimaryPath = false;
        if (!string.IsNullOrEmpty(socketDir))
        {
            if (Directory.Exists(socketDir))
            {
                usePrimaryPath = true;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(socketDir);
                    usePrimaryPath = true;
                    Log.Information("Created socket directory: {Dir}", socketDir);
                }
                catch (UnauthorizedAccessException)
                {
                    // Can't create /run/crossmacro - likely not running via systemd
                    Log.Warning("Cannot create {Dir}, falling back to /tmp", socketDir);
                }
            }
        }

        // Fallback to /tmp for portable / AppImage deployments
        if (!usePrimaryPath)
        {
            socketPath = IpcProtocol.FallbackSocketPath;
            socketDir = Path.GetDirectoryName(socketPath);
            Log.Information("Using fallback socket path: {Path}", socketPath);
        }

        if (File.Exists(socketPath))
        {
            try
            {
                File.Delete(socketPath);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to cleanup existing socket: {Msg}", ex.Message);
            }
        }

        try
        {
            _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _socket.Bind(new UnixDomainSocketEndPoint(socketPath));
            _socket.Listen(1);
            
            // Socket Permissions / Ownership Logic
            // The permission service handles socket-specific permissions which is separate from general input permissions.
            // Note: We might want to use _permissionService.CheckUInputAccess() here to fail early if we lack input rights,
            // but the Daemon's main job is IPC, so we let it start.
            _permissionService.ConfigureSocketPermissions(socketPath);

            Log.Information("Listening on {SocketPath}", socketPath);
            
            // Signal systemd that we're ready to accept connections
            SystemdNotify.Ready();
            SystemdNotify.Status("Listening for client connections");

            // Allow concurrent connections? No, strictly one controller.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _socket.AcceptAsync(token);
                    
                    // Validate
                    var validationResult = await _security.ValidateConnectionAsync(client);
                    if (validationResult == null)
                    {
                        // Rejected in validation (logging done there)
                        continue; // client disposed in security service
                    }
                    
                    var (uid, pid) = validationResult.Value;
                    var sessionStart = DateTime.UtcNow;

                    try 
                    {
                        // Create Session Handler (Composition Root for Session)
                        var handler = new SessionHandler(_security, _virtualDevice, _inputCapture);
                        await handler.RunAsync(client, uid, pid, token);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Client session error");
                    }
                    finally
                    {
                        var duration = DateTime.UtcNow - sessionStart;
                        _security.LogDisconnect(uid, pid, duration);
                        client.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Accept failed");
                }
            }
        } // End of top-level try
        finally
        {
            if (File.Exists(socketPath))
            {
                try
                {
                    File.Delete(socketPath);
                    Log.Information("Socket cleaned up");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to clean up socket on exit");
                }
            }
            _socket?.Dispose();
        }
    }


}
