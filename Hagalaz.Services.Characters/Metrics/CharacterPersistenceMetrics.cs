using System.Diagnostics.Metrics;

namespace Hagalaz.Services.Characters.Metrics;

/// <summary>
/// Operational counters for the durable character persistence consumer.
/// </summary>
public sealed class CharacterPersistenceMetrics
{
    public const string MeterName = "Hagalaz.Services.Characters.Persistence";

    private static readonly Meter Meter = new(MeterName);
    private readonly Counter<long> _applied = Meter.CreateCounter<long>("hagalaz.character.persistence.applied");
    private readonly Counter<long> _duplicateOrStale = Meter.CreateCounter<long>("hagalaz.character.persistence.duplicate_or_stale");
    private readonly Counter<long> _failures = Meter.CreateCounter<long>("hagalaz.character.persistence.failures");
    private readonly Counter<long> _unknownCharacters = Meter.CreateCounter<long>("hagalaz.character.persistence.unknown_character");

    public void RecordApplied() => _applied.Add(1);

    public void RecordDuplicateOrStale() => _duplicateOrStale.Add(1);

    public void RecordFailure() => _failures.Add(1);

    public void RecordUnknownCharacter() => _unknownCharacters.Add(1);
}
