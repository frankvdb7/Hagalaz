namespace Raido.Server;

/// <summary>
/// Describes the lifetime state of a Raido logical connection.
/// </summary>
public enum RaidoConnectionLifecycleState
{
    Connected,
    Reconnecting,
    Closed
}
