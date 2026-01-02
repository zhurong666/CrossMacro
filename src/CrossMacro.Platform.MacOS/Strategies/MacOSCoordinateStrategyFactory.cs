using System;
using CrossMacro.Core.Services;
using CrossMacro.Core.Services.Recording.Strategies;

namespace CrossMacro.Platform.MacOS.Strategies;

public class MacOSCoordinateStrategyFactory : ICoordinateStrategyFactory
{
    private readonly IMousePositionProvider _positionProvider;
    private readonly Func<IInputSimulator>? _inputSimulatorFactory;

    public MacOSCoordinateStrategyFactory(
        IMousePositionProvider positionProvider,
        Func<IInputSimulator>? inputSimulatorFactory = null)
    {
        _positionProvider = positionProvider;
        _inputSimulatorFactory = inputSimulatorFactory;
    }

    public ICoordinateStrategy Create(bool useAbsoluteCoordinates, bool forceRelative, bool skipInitialZero)
    {
        if (forceRelative)
        {
            return new RelativeCoordinateStrategy();
        }

        if (useAbsoluteCoordinates)
        {
             return new AbsoluteCoordinateStrategy(_positionProvider);
        }
        else
        {
             return new RelativeCoordinateStrategy();
        }
    }
}
