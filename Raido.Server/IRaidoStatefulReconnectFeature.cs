namespace Raido.Server
{
    /// <summary>
    /// Provides application control over whether an opted-in logical connection may be retained after transport loss.
    /// </summary>
    public interface IRaidoStatefulReconnectFeature
    {
        /// <summary>
        /// Permanently disables stateful reconnect for the logical connection.
        /// </summary>
        void DisableReconnect();
    }
}
