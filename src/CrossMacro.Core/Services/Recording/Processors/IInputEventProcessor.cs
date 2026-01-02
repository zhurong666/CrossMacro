using System.Collections.Generic;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;

namespace CrossMacro.Core.Services.Recording.Processors;

public interface IInputEventProcessor
{
    /// <summary>
    /// Configures the processor with recording preferences.
    /// </summary>
    void Configure(bool recordMouse, bool recordKeyboard, HashSet<int>? ignoredKeys, bool isAbsoluteCoordinates = false);

    /// <summary>
    /// Processes a raw input capture event and converts it into a MacroEvent.
    /// Returns null if the event should be ignored.
    /// </summary>
    /// <param name="args">The raw input arguments.</param>
    /// <param name="timestamp">The timestamp for the event.</param>
    /// <returns>A MacroEvent if valid, otherwise null.</returns>
    MacroEvent? Process(InputCaptureEventArgs args, long timestamp);
}
