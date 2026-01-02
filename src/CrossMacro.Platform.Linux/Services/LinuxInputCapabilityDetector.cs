using System.IO;
using CrossMacro.Core.Ipc;
using Serilog;

namespace CrossMacro.Platform.Linux.Services;

/// <summary>
/// Detects system capabilities and determines the appropriate input provider mode.
/// Implements thread-safe, cached detection for daemon connectivity and uinput access.
/// </summary>
public class LinuxInputCapabilityDetector : ILinuxInputCapabilityDetector
{
    private InputProviderMode? _cachedMode;
    private bool? _canConnectToDaemon;
    private bool? _canUseDirectUInput;
    private readonly Lock _lock = new();
    
    public bool CanConnectToDaemon
    {
        get
        {
            if (_canConnectToDaemon.HasValue)
                return _canConnectToDaemon.Value;
            
            using (_lock.EnterScope())
            {
                if (_canConnectToDaemon.HasValue)
                    return _canConnectToDaemon.Value;
                
                try
                {
                    _canConnectToDaemon = File.Exists(IpcProtocol.DefaultSocketPath) ||
                                          File.Exists(IpcProtocol.FallbackSocketPath);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "[LinuxInputCapabilityDetector] Failed to check daemon socket paths");
                    _canConnectToDaemon = false;
                }
                
                return _canConnectToDaemon.Value;
            }
        }
    }
    
    public bool CanUseDirectUInput
    {
        get
        {
            if (_canUseDirectUInput.HasValue)
                return _canUseDirectUInput.Value;
            
            using (_lock.EnterScope())
            {
                if (_canUseDirectUInput.HasValue)
                    return _canUseDirectUInput.Value;
                
                try
                {
                    if (File.Exists(LinuxConstants.UInputDevicePath))
                    {
                        using (File.OpenWrite(LinuxConstants.UInputDevicePath)) 
                        { 
                            _canUseDirectUInput = true; 
                        }
                    }
                    else
                    {
                        _canUseDirectUInput = false;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _canUseDirectUInput = false;
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "[LinuxInputCapabilityDetector] Failed to check {UInputPath} access", LinuxConstants.UInputDevicePath);
                    _canUseDirectUInput = false;
                }
                
                return _canUseDirectUInput.Value;
            }
        }
    }
    
    public InputProviderMode DetermineMode()
    {
        if (_cachedMode.HasValue)
            return _cachedMode.Value;
        
        using (_lock.EnterScope())
        {
            if (_cachedMode.HasValue)
                return _cachedMode.Value;
            
            if (CanConnectToDaemon)
            {
                _cachedMode = InputProviderMode.Daemon;
                return InputProviderMode.Daemon;
            }

            if (CanUseDirectUInput)
            {
                Log.Warning("[LinuxInputCapabilityDetector] Daemon socket not found, but we have {UInputPath} access. Using LEGACY mode.", LinuxConstants.UInputDevicePath);
                _cachedMode = InputProviderMode.Legacy;
                return InputProviderMode.Legacy;
            }

            // Default to LEGACY mode as a last resort
            Log.Warning("[LinuxInputCapabilityDetector] Neither Daemon socket nor {UInputPath} write access found. Defaulting to LEGACY mode (expect permission errors if not running as root/input group).", LinuxConstants.UInputDevicePath);
            _cachedMode = InputProviderMode.Legacy;
            return InputProviderMode.Legacy;
        }
    }
}
