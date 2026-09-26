using Xunit;

namespace Hagalaz.Cache.Tests;

[CollectionDefinition("Cache API telemetry", DisableParallelization = true)]
public sealed class CacheApiTelemetryCollection
{
    public const string Name = "Cache API telemetry";
}
