using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for a Runecrafting altar.
    /// </summary>
    public record RunecraftingDto
    {
        /// <summary>
        /// Gets the object ID of the altar.
        /// </summary>
        public required int AltarId { get; init; }

        /// <summary>
        /// Gets the object ID of the mysterious ruins.
        /// </summary>
        public required int RuinId { get; init; }

        /// <summary>
        /// Gets the object ID of the portal within the ruins.
        /// </summary>
        public required int PortalId { get; init; }

        /// <summary>
        /// Gets the object ID of the rift.
        /// </summary>
        public required int RiftId { get; init; }

        /// <summary>
        /// Gets the item ID of the rune crafted at this altar.
        /// </summary>
        public required int RuneId { get; init; }

        /// <summary>
        /// Gets the item ID of the talisman for this altar.
        /// </summary>
        public required int TalismanId { get; init; }

        /// <summary>
        /// Gets the item ID of the tiara for this altar.
        /// </summary>
        public required int TiaraId { get; init; }

        /// <summary>
        /// Gets the required Runecrafting level to use this altar.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// Gets the Runecrafting experience gained per essence.
        /// </summary>
        public required double Experience { get; init; }

        /// <summary>
        /// Gets an array of level thresholds for crafting multiple runes.
        /// </summary>
        public required int[] LevelCountMultipliers { get; init; }

        /// <summary>
        /// Gets the location of the altar.
        /// </summary>
        public required ILocation AltarLocation { get; init; }

        /// <summary>
        /// Gets the location of the mysterious ruins.
        /// </summary>
        public required ILocation RuinLocation { get; init; }

        /// <summary>
        /// Gets the location of the rift.
        /// </summary>
        public required ILocation RiftLocation { get; init; }
    }
}