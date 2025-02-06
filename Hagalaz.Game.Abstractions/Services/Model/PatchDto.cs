using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record PatchDto
    {
        /// <summary>
        /// The object identifier.
        /// </summary>
        public required int ObjectID;

        /// <summary>
        /// The type.
        /// </summary>
        public required PatchType Type;
    }
}