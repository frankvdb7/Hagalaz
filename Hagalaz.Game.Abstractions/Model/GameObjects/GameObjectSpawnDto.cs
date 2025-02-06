namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Class which contains data about game object spawn.
    /// </summary>
    public record GameObjectSpawnDto
    {
        /// <summary>
        /// Id of the object.
        /// </summary>
        public int ObjectId { get; init; }

        /// <summary>
        /// Type of the object.
        /// </summary>
        public ShapeType ShapeType { get; init; }

        /// <summary>
        /// Rotation of the object.
        /// </summary>
        public int Rotation { get; init; }

        /// <summary>
        /// Location of the object.
        /// </summary>
        public ILocation Location { get; set; } = default!;
    }
}