using CrossMacro.Core.Services;
using CrossMacro.Platform.Linux.DisplayServer;

namespace CrossMacro.Platform.Linux.Services.Factories.Selectors;

public class FallbackPositionProviderSelector : IPositionProviderSelector
{
    // Lowest priority to act as default
    public int Priority => 0;

    public bool CanHandle(CompositorType compositor)
    {
        // Can handle anything that others didn't pick up
        return true;
    }

    public IMousePositionProvider Create()
    {
        return new FallbackPositionProvider();
    }
}
