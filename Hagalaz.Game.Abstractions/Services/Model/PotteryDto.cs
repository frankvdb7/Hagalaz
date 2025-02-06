namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record PotteryDto
    {
        /// <summary>
        /// The product identifier
        /// </summary>
        public required int FormedProductID;

        /// <summary>
        /// The final product identifier
        /// </summary>
        public required int BakedProductID;

        /// <summary>
        /// The required level
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The form experience
        /// </summary>
        public required double FormExperience;

        /// <summary>
        /// The bake experience
        /// </summary>
        public required double BakeExperience;
    }
}