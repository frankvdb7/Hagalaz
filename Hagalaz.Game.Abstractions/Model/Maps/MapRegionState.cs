namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Describes the initial-load lifecycle of a map region instance.
    /// </summary>
    public enum MapRegionState
    {
        /// <summary>
        /// The region may still complete its initial loading.
        /// </summary>
        Initializing,

        /// <summary>
        /// Initial loading and population completed successfully.
        /// </summary>
        Ready,

        /// <summary>
        /// Initial loading failed or was canceled and this instance is terminal.
        /// </summary>
        Discarded
    }
}
