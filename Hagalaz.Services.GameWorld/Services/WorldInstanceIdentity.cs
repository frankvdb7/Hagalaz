using System;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class WorldInstanceIdentity
{
    public WorldInstanceIdentity()
    {
        StartedAt = DateTimeOffset.UtcNow;
        InstanceId = Guid.NewGuid().ToString("N");
        Generation = StartedAt.UtcTicks;
    }

    public string InstanceId { get; }
    public long Generation { get; }
    public DateTimeOffset StartedAt { get; }
}
