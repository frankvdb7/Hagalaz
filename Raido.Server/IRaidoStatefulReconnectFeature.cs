namespace Raido.Server;

/// <summary>
/// Controls whether the stable logical connection may accept a stateful reconnect.
/// </summary>
public interface IRaidoStatefulReconnectFeature
{
    /// <summary>
    /// Enables stateful reconnect for the logical connection.
    /// </summary>
    /// <returns><see langword="true"/> when reconnect is enabled; otherwise, <see langword="false"/> if the connection is terminal.</returns>
    bool TryEnable();
}
