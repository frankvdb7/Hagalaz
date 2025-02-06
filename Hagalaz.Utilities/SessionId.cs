using System;

namespace Hagalaz.Utilities
{
    public static class SessionId
    {
        public static long NewId() => ((long)(Random.Shared.NextDouble() * 99999999D) << 32) + (long)(Random.Shared.NextDouble() * 99999999D);
    }
}