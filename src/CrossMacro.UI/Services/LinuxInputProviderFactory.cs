using System;
using System.IO;
using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux;
using CrossMacro.Platform.Linux.Ipc;
using Serilog;

namespace CrossMacro.UI.Services;

/// <summary>
/// Factory to create Linux Input Simulator and Capture implementations based on system capabilities.
/// Supports Hybrid Mode (Daemon Priority -> Direct/Root Fallback).
/// </summary>
public class LinuxInputProviderFactory
{
    private readonly IpcClient _ipcClient;
    private readonly Func<LinuxInputSimulator> _legacySimulatorFactory;
    private readonly Func<LinuxInputCapture> _legacyCaptureFactory;
    private readonly Func<LinuxIpcInputSimulator> _ipcSimulatorFactory;
    private readonly Func<LinuxIpcInputCapture> _ipcCaptureFactory;
    private readonly Func<CrossMacro.Platform.Linux.Services.X11InputSimulator> _x11SimulatorFactory;
    private readonly Func<CrossMacro.Platform.Linux.Services.X11InputCapture> _x11CaptureFactory;

    // Cache the decision to ensure consistent behavior
    private bool? _useLegacy;
    
    public LinuxInputProviderFactory(
        IpcClient ipcClient,
        Func<LinuxInputSimulator> legacySimulatorFactory,
        Func<LinuxInputCapture> legacyCaptureFactory,
        Func<LinuxIpcInputSimulator> ipcSimulatorFactory,
        Func<LinuxIpcInputCapture> ipcCaptureFactory,
        Func<CrossMacro.Platform.Linux.Services.X11InputSimulator> x11SimulatorFactory,
        Func<CrossMacro.Platform.Linux.Services.X11InputCapture> x11CaptureFactory)
    {
        _ipcClient = ipcClient;
        _legacySimulatorFactory = legacySimulatorFactory;
        _legacyCaptureFactory = legacyCaptureFactory;
        _ipcSimulatorFactory = ipcSimulatorFactory;
        _ipcCaptureFactory = ipcCaptureFactory;
        _x11SimulatorFactory = x11SimulatorFactory;
        _x11CaptureFactory = x11CaptureFactory;
    }

    // Log suppression flags
    private static bool _loggedPositionProvider;
    private static bool _loggedSimulator;
    private static bool _loggedCapture;

    public IMousePositionProvider CreatePositionProvider()
    {
        // Use existing detection logic (moved from MousePositionProviderFactory)
        var compositorType = CrossMacro.Platform.Linux.DisplayServer.CompositorDetector.DetectCompositor();
        
        if (!_loggedPositionProvider)
        {
            Log.Information("[LinuxInputFactory] DetectCompositor: {Compositor}", compositorType);
            _loggedPositionProvider = true;
        }

        return compositorType switch
        {
            CrossMacro.Platform.Linux.DisplayServer.CompositorType.X11 => new CrossMacro.Platform.Linux.DisplayServer.X11.X11PositionProvider(),
            CrossMacro.Platform.Linux.DisplayServer.CompositorType.HYPRLAND => new CrossMacro.Platform.Linux.DisplayServer.Wayland.HyprlandPositionProvider(),
            CrossMacro.Platform.Linux.DisplayServer.CompositorType.KDE => new CrossMacro.Platform.Linux.DisplayServer.Wayland.KdePositionProvider(),
            CrossMacro.Platform.Linux.DisplayServer.CompositorType.GNOME => new CrossMacro.Platform.Linux.DisplayServer.Wayland.GnomePositionProvider(),
            _ => new CrossMacro.Platform.Linux.DisplayServer.FallbackPositionProvider()
        };
    }

    public IInputSimulator CreateSimulator()
    {
        var compositor = CrossMacro.Platform.Linux.DisplayServer.CompositorDetector.DetectCompositor();
        
        // 1. Wayland / Hybrid -> Force Daemon (IPC)
        // Note: GNOME/KDE/HYPRLAND types in CompositorDetector implies Wayland session
        if (IsWayland(compositor))
        {
            if (!_loggedSimulator)
            {
                Log.Information("[LinuxInputFactory] Wayland detected ({Comp}), using Daemon Input Simulator", compositor);
                _loggedSimulator = true;
            }
            return _ipcSimulatorFactory();
        }

        // 2. X11 / Unknown -> Try Native X11
        var x11Sim = _x11SimulatorFactory();
        if (x11Sim.IsSupported)
        {
            if (!_loggedSimulator)
            {
                Log.Information("[LinuxInputFactory] X11 detected, using Native X11 Input Simulator");
                _loggedSimulator = true;
            }
            return x11Sim;
        }

        // 3. Fallback (Native X11 failed or unsupported) -> Legacy/Daemon
        // This covers cases where standard X11 extensions are missing or we are in a weird state.
        if (!_loggedSimulator)
        {
            Log.Information("[LinuxInputFactory] Fallback: Using Daemon/Legacy Input Simulator");
            _loggedSimulator = true;
        }
        
        // Check if we should use Direct UInput (AppImage/Root case) or IPC
        if (ShouldUseLegacy())
        {
             return _legacySimulatorFactory();
        }
        return _ipcSimulatorFactory();
    }

    public IInputCapture CreateCapture()
    {
        var compositor = CrossMacro.Platform.Linux.DisplayServer.CompositorDetector.DetectCompositor();

        // 1. Wayland / Hybrid -> Force Daemon (IPC)
        if (IsWayland(compositor))
        {
            if (!_loggedCapture)
            {
                Log.Information("[LinuxInputFactory] Wayland detected ({Comp}), using Daemon Input Capture", compositor);
                _loggedCapture = true;
            }
            return _ipcCaptureFactory();
        }

        // 2. X11 / Unknown -> Try Native X11
        var x11Cap = _x11CaptureFactory();
        if (x11Cap.IsSupported)
        {
            if (!_loggedCapture)
            {
                Log.Information("[LinuxInputFactory] X11 detected, using Native X11 Input Capture");
                _loggedCapture = true;
            }
            return x11Cap;
        }

        // 3. Fallback
        if (!_loggedCapture)
        {
            Log.Information("[LinuxInputFactory] Fallback: Using Daemon/Legacy Input Capture");
            _loggedCapture = true;
        }

        if (ShouldUseLegacy())
        {
            return _legacyCaptureFactory();
        }
        return _ipcCaptureFactory();
    }

    private bool IsWayland(CrossMacro.Platform.Linux.DisplayServer.CompositorType type)
    {
        return type == CrossMacro.Platform.Linux.DisplayServer.CompositorType.HYPRLAND ||
               type == CrossMacro.Platform.Linux.DisplayServer.CompositorType.GNOME ||
               type == CrossMacro.Platform.Linux.DisplayServer.CompositorType.KDE ||
               type == CrossMacro.Platform.Linux.DisplayServer.CompositorType.Other;
    }

    private bool ShouldUseLegacy()
    {
        if (_useLegacy.HasValue) return _useLegacy.Value;

        // 1. Check if we are Root (UID 0) or effectively have permission
        // NOTE: Even if we are not root, if we are in 'input' group, we might be able to use legacy.
        // But the main differentiator is: Can we connect to the daemon?
        
        bool canConnectToDaemon = false;
        try 
        {
            // Check both primary and fallback socket paths
            if (File.Exists(CrossMacro.Core.Ipc.IpcProtocol.DefaultSocketPath) ||
                File.Exists(CrossMacro.Core.Ipc.IpcProtocol.FallbackSocketPath))
            {
                canConnectToDaemon = true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "[LinuxInputFactory] Failed to check daemon socket paths");
        }

        // 2. Check if we can write to uinput (Legacy Requirement)
        bool canUseDirectUInput = false;
        try
        {
            if (File.Exists("/dev/uinput"))
            {
                 // Try to open for write? Or just assume if writable.
                 // For safety, let's assume if socket exists, we prefer daemon.
                 // If socket MISSING, we fallback.
                 using (File.OpenWrite("/dev/uinput")) { canUseDirectUInput = true; }
            }
        }
        catch (UnauthorizedAccessException) 
        {
            canUseDirectUInput = false;
        }
        catch (Exception ex)
        { 
             Log.Debug(ex, "[LinuxInputFactory] Failed to check /dev/uinput access");
        }

        if (canConnectToDaemon)
        {
            _useLegacy = false;
            return false;
        }

        if (canUseDirectUInput)
        {
            Log.Warning("[LinuxInputFactory] Daemon socket not found, but we have /dev/uinput access. Falling back to LEGACY mode.");
            _useLegacy = true;
            return true;
        }

        // If neither found, default to LEGACY mode as a last resort for portability (AppImage/Native).
        // This allows the application to attempt direct access, which might work if permissions are granted via other means (e.g. capabilities),
        // or will produce a clear "Permission Denied" error instead of a confusing "Daemon Not Found".
        Log.Warning("[LinuxInputFactory] Neither Daemon socket nor /dev/uinput write access found. Defaulting to LEGACY mode (expect permission errors if not running as root/input group).");
        _useLegacy = true;
        return true;
    }
}
