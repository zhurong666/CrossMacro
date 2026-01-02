using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.DisplayServer;
using CrossMacro.Platform.Linux.DisplayServer.Wayland;

namespace CrossMacro.Platform.Linux.Services.Factories.Selectors;

public class KdePositionProviderSelector : IPositionProviderSelector
{
    public int Priority => 10;

    public bool CanHandle(CompositorType compositor)
    {
        return compositor == CompositorType.KDE;
    }

    public IMousePositionProvider Create()
    {
        return new KdePositionProvider();
    }
}
