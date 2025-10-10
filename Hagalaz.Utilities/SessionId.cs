using System;

namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides a utility for generating unique session identifiers.
    /// </summary>
    public static class SessionId
    {
        /// <summary>
        /// Generates a new, pseudo-random 64-bit session identifier.
        /// </summary>
        /// <returns>A <see cref="long"/> representing the new session ID.</returns>
        public static long NewId() => ((long)(Random.Shared.NextDouble() * 99999999D) << 32) + (long)(Random.Shared.NextDouble() * 99999999D);
    }
}