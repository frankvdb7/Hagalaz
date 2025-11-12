namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a lodestone in the lodestone network.
    /// </summary>
    public record LodestoneDto
    {
        /// <summary>
        /// Gets the game object ID of the lodestone.
        /// </summary>
        public required int GameObjectId { get; init; }

        /// <summary>
        /// Gets the widget component ID for this lodestone in the teleportation interface.
        /// </summary>
        public required int ComponentId { get; init; }

        /// <summary>
        /// Gets the state that represents the activation of this lodestone.
        /// </summary>
        public required string StateId { get; init; }

        /// <summary>
        /// Gets the X-coordinate of the teleport destination.
        /// </summary>
        public required int CoordX { get; init; }

        /// <summary>
        /// Gets the Y-coordinate of the teleport destination.
        /// </summary>
        public required int CoordY { get; init; }

        /// <summary>
        /// Gets the Z-coordinate (plane) of the teleport destination.
        /// </summary>
        public required int CoordZ { get; init; }
    }
}