namespace Raido.Server;

/// <summary>
/// Exposes the physical connection session that owns the current Kestrel connection.
/// </summary>
public interface IRaidoPhysicalConnectionFeature
{
    RaidoPhysicalConnectionSession Session { get; }
}
