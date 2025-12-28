using System;
using Serilog;

namespace CrossMacro.Platform.Linux.DisplayServer
{
    /// <summary>
    /// Detects the currently running display server / compositor
    /// </summary>
    public static class CompositorDetector
    {
        /// <summary>
        /// Detects the current compositor by checking environment variables
        /// </summary>
        public static CompositorType DetectCompositor()
        {
            // Check session type (X11 vs Wayland)
            var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            var x11Display = Environment.GetEnvironmentVariable("DISPLAY");
            
            var isWayland = !string.IsNullOrEmpty(waylandDisplay) || 
                           string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase);
            
            var isX11 = !string.IsNullOrEmpty(x11Display) ||
                       string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase);

            // Prioritize X11 detection if both are present (XWayland scenario)
            if (isX11 && !isWayland)
            {
                Log.Information("[CompositorDetector] X11 session detected");
                return CompositorType.X11;
            }
            
            if (!isWayland)
            {
                Log.Warning("[CompositorDetector] No known display server detected");
                return CompositorType.Unknown;
            }

            // Detect specific compositor
            var currentDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";

            return currentDesktop.ToUpperInvariant() switch
            {
                var desktop when desktop.Contains("HYPRLAND") => 
                    LogAndReturn(CompositorType.HYPRLAND, "Hyprland"),
                
                "KDE" => 
                    LogAndReturn(CompositorType.KDE, "KDE Plasma"),
                
                var desktop when desktop.Contains("GNOME") => 
                    LogAndReturn(CompositorType.GNOME, "GNOME"),
                
                _ when isWayland => 
                    LogAndReturnUnknown(currentDesktop),
                
                _ => CompositorType.Unknown
            };
        }

        private static CompositorType LogAndReturn(CompositorType type, string name)
        {
            Log.Information("[CompositorDetector] Detected {Compositor}", name);
            return type;
        }

        private static CompositorType LogAndReturnUnknown(string desktop)
        {
            Log.Information("[CompositorDetector] Wayland session detected but specific compositor unknown (Desktop: {Desktop})", desktop);
            return CompositorType.Other;
        }
    }
}
