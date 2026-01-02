using CrossMacro.Core.Services;
using CrossMacro.Core.Services.Recording.Strategies;
using CrossMacro.Platform.Linux.Strategies;

namespace CrossMacro.Platform.Linux.Strategies.Selectors;

public class WaylandAbsoluteStrategySelector : ICoordinateStrategySelector
{
    private readonly IMousePositionProvider _positionProvider;

    public WaylandAbsoluteStrategySelector(IMousePositionProvider positionProvider)
    {
        _positionProvider = positionProvider;
    }

    public int Priority => 10;

    public bool CanHandle(StrategyContext context)
    {
        return context.IsWayland && context.UseAbsoluteCoordinates;
    }

    public ICoordinateStrategy Create(StrategyContext context)
    {
        return new EvdevAbsoluteStrategy(_positionProvider);
    }
}
