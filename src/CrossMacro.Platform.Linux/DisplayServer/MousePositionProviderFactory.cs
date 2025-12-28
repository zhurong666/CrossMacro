using Serilog;
using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.DisplayServer.Wayland;
using CrossMacro.Platform.Linux.DisplayServer.X11;

namespace CrossMacro.Platform.Linux.DisplayServer
{
    /// <summary>
    /// Factory for creating the appropriate mouse position provider based on the detected display server
    /// </summary>
    public static class MousePositionProviderFactory
    {
        /// <summary>
        /// Create a position provider based on the current Wayland compositor
        /// </summary>
        public static IMousePositionProvider CreateProvider()
        {
            var compositorType = CompositorDetector.DetectCompositor();
            
            Log.Information("[MousePositionProviderFactory] Detected compositor: {CompositorType}", compositorType);
            
            IMousePositionProvider provider = compositorType switch
            {
                CompositorType.X11 => new X11PositionProvider(),
                CompositorType.HYPRLAND => new HyprlandPositionProvider(),
                CompositorType.KDE => new KdePositionProvider(),
                CompositorType.GNOME => new GnomePositionProvider(),
                CompositorType.Other => new FallbackPositionProvider(),
                CompositorType.Unknown => new FallbackPositionProvider(),
                _ => new FallbackPositionProvider()
            };
            
            if (provider.IsSupported)
            {
                Log.Information("[MousePositionProviderFactory] Using provider: {ProviderName}", provider.ProviderName);
            }
            else
            {
                Log.Warning("[MousePositionProviderFactory] Provider not supported, falling back to relative coordinates");
            }
            
            return provider;
        }
    }
}
