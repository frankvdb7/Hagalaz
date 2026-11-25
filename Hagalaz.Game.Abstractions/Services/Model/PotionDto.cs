namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the persisted definition for making a potion.
    /// </summary>
    public record PotionDto
    {
        /// <summary>
        /// The item ID of the finished potion.
        /// </summary>
        public required int PotionId { get; init; }

        /// <summary>
        /// How the recipe consumes its ingredients.
        /// </summary>
        public required PotionKind Kind { get; init; }

        /// <summary>
        /// The item ID the secondary ingredients are added to: the unfinished potion, or the herb for an overload.
        /// </summary>
        public required int UnfinishedPotionId { get; init; }

        /// <summary>
        /// The required Herblore level to make this potion.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// The Herblore experience gained for making this potion.
        /// </summary>
        public required double Experience { get; init; }

        /// <summary>
        /// The item IDs of the primary ingredients that turn a vial of water into the unfinished potion.
        /// </summary>
        public required int[] PrimaryItemIds { get; init; }

        /// <summary>
        /// The item IDs of the secondary ingredients.
        /// </summary>
        public required int[] SecondaryItemIds { get; init; }

        /// <summary>
        /// The amount of the secondary ingredient consumed per potion.
        /// </summary>
        public required int SecondaryItemCount { get; init; }
    }
}
