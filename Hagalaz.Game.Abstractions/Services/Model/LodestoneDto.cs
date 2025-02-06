using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record LodestoneDto
    {
        /// <summary>
        /// The game object identifier.
        /// </summary>
        public required int GameObjectId { get; init; }

        /// <summary>
        /// The component identifier.
        /// </summary>
        public required int ComponentId { get; init; }

        /// <summary>
        /// The state.
        /// </summary>
        public required StateType State { get; init; }

        /// <summary>
        /// The coord x.
        /// </summary>
        public required int CoordX { get; init; }

        /// <summary>
        /// The coord y.
        /// </summary>
        public required int CoordY { get; init; }

        /// <summary>
        /// The coord z.
        /// </summary>
        public required int CoordZ { get; init; }

    }
}