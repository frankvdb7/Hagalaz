namespace Raido.Server
{
    /// <summary>
    /// A hub activator for creating and releasing hub instances.
    /// </summary>
    /// <typeparam name="THub">The type of the hub.</typeparam>
    public interface IRaidoHubActivator<THub> where THub : RaidoHub
    {
        /// <summary>
        /// Creates a new hub instance.
        /// </summary>
        /// <returns>A new hub instance.</returns>
        THub Create();

        /// <summary>
        /// Releases a hub instance.
        /// </summary>
        /// <param name="hub">The hub instance to release.</param>
        void Release(THub hub);
    }
}