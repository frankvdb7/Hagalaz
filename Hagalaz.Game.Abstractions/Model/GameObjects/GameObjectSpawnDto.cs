namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// A data transfer object containing information required to spawn a game object.
    /// </summary>
    public record GameObjectSpawnDto
    {
        /// <summary>
        /// Gets the ID of the game object to spawn.
        /// </summary>
        public int ObjectId { get; init; }

        /// <summary>
        /// Gets the shape type of the game object.
        /// </summary>
        public ShapeType ShapeType { get; init; }

        /// <summary>
        /// Gets the rotation of the game object.
        /// </summary>
        public int Rotation { get; init; }

        /// <summary>
        /// Gets or sets the spawn location of the game object.
        /// </summary>
        public ILocation Location { get; set; } = default!;
    }
}