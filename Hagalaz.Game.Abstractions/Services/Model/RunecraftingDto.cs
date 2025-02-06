using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record RunecraftingDto
    {
        /// <summary>
        /// The altar identifier.
        /// </summary>
        public required int AltarId { get; init; }

        /// <summary>
        /// The ruin identifier.
        /// </summary>
        public required int RuinId { get; init; }

        /// <summary>
        /// The portal identifier.
        /// </summary>
        public required int PortalId { get; init; }

        /// <summary>
        /// The rift identifier.
        /// </summary>
        public required int RiftId
        {
            get; init;
        }

        /// <summary>
        /// The rune identifier.
        /// </summary>
        public required int RuneId
        {
            get; init;
        }

        /// <summary>
        /// The talisman identifier.
        /// </summary>
        public required int TalismanId
        {
            get; init;
        }

        /// <summary>
        /// The tiara identifier.
        /// </summary>
        public required int TiaraId
        {
            get; init;
        }

        /// <summary>
        /// The required level.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// The experience.
        /// </summary>
        public required double Experience { get; init; }

        /// <summary>
        /// The level count multipliers.
        /// </summary>
        public required int[] LevelCountMultipliers { get; init; }

        /// <summary>
        /// The altar location.
        /// </summary>
        public required ILocation AltarLocation { get; init; }

        /// <summary>
        /// The ruin location.
        /// </summary>
        public required ILocation RuinLocation { get; init; }

        /// <summary>
        /// The rift location.
        /// </summary>
        public required ILocation RiftLocation { get; init; }
    }
}