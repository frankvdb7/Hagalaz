using System;

namespace Hagalaz.Services.GameWorld.Store;

public static class GameSessionAbortOptions
{
    /// <summary>
    /// Bounds a local abort-processing reservation independently of the distributed
    /// world-session claim lease. A processor that exceeds this duration can be
    /// replaced, but its generation token can no longer complete that replacement.
    /// </summary>
    public static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);
}
