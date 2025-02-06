namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record PrayerDto
    {
        /// <summary>
        /// The item identifier
        /// </summary>
        public required int ItemId;

        /// <summary>
        /// The experience
        /// </summary>
        public required double Experience;

        /// <summary>
        /// The type
        /// </summary>
        public required PrayerDtoType Type;
    }
}