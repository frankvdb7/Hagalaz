using System;
using System.Threading;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed class SnapshotRevisionGenerator
    {
        private long _lastRevision;

        public long Next()
        {
            var timestamp = DateTime.UtcNow.Ticks;
            while (true)
            {
                var previous = Interlocked.Read(ref _lastRevision);
                var next = Math.Max(timestamp, previous + 1);
                if (Interlocked.CompareExchange(ref _lastRevision, next, previous) == previous)
                {
                    return next;
                }
            }
        }
    }
}
