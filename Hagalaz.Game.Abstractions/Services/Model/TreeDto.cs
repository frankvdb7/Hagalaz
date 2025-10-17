namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a tree for the Woodcutting skill.
    /// </summary>
    public record TreeDto
    {
        /// <summary>
        /// Gets the game object ID of the tree.
        /// </summary>
        public required int Id { get; init; }

        /// <summary>
        /// Gets the game object ID of the tree stump that appears after the tree is cut down.
        /// </summary>
        public required int StumpId { get; init; }
    }
}