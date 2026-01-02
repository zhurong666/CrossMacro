using System;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;

namespace CrossMacro.Core.Services.Recording.Strategies;

public interface ICoordinateStrategy : IDisposable
{
    /// <summary>
    /// Initialize the strategy (e.g., find initial position, perform corner reset).
    /// </summary>
    Task InitializeAsync(CancellationToken ct);

    /// <summary>
    /// Process an input event to determine the recorded position.
    /// Use this to return calculated absolute coordinates or relative deltas.
    /// </summary>
    /// <param name="e">The raw input event.</param>
    /// <returns>A tuple of (X, Y) which can be absolute or relative depending on the strategy.</returns>
    (int X, int Y) ProcessPosition(InputCaptureEventArgs e);
}
