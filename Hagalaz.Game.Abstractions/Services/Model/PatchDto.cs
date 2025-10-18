using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a farming patch.
    /// </summary>
    public record PatchDto
    {
        /// <summary>
        /// The game object ID of the farming patch.
        /// </summary>
        public required int ObjectID;

        /// <summary>
        /// The type of the farming patch (e.g., Allotment, Tree, Herb).
        /// </summary>
        public required PatchType Type;
    }
}