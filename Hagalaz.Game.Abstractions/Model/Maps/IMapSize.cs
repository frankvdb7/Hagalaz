namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for an object that represents a map size, typically for a viewport.
    /// </summary>
    public interface IMapSize
    {
        /// <summary>
        /// Gets the type of the map size.
        /// </summary>
        int Type { get; }

        /// <summary>
        /// Gets the size value.
        /// </summary>
        int Size { get; }
    }
}