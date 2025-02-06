namespace Hagalaz.Game.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class GroundItemOptions
    {
        public const string Key = "GroundItem";

        /// <summary>
        /// 
        /// </summary>
        public int PublicTickTime { get; set; } = 100;

        /// <summary>
        /// 
        /// </summary>
        public int PrivateTickTime { get; set; } = 100;

        /// <summary>
        /// 
        /// </summary>
        public int NonTradableTickTime { get; set; } = 100;
    }
}