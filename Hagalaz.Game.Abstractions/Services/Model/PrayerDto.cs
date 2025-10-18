namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for a prayer-related item, such as bones or ashes.
    /// </summary>
    public record PrayerDto
    {
        /// <summary>
        /// The item ID of the bones or ashes.
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The Prayer experience gained for using this item.
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The type of prayer item.
        /// </summary>
        public required PrayerDtoType Type;
    }
}