using CrossMacro.Core.Services.Recording.Strategies;

namespace CrossMacro.Core.Services.Recording.Strategies;

public interface ICoordinateStrategyFactory
{
    /// <summary>
    /// Creates a coordinate strategy based on the requested mode and current platform capabilities.
    /// </summary>
    /// <param name="useAbsoluteCoordinates">Whether absolute coordinates are requested.</param>
    /// <param name="forceRelative">Whether to force relative mode (Blind Mode).</param>
    /// <param name="skipInitialZero">Whether to skip initial zero reset (prevent jumping to 0,0).</param>
    /// <returns>An appropriate ICoordinateStrategy implementation.</returns>
    ICoordinateStrategy Create(bool useAbsoluteCoordinates, bool forceRelative, bool skipInitialZero);
}
