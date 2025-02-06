namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record SilverDto
    {
        /// <summary>
        /// The child identifier
        /// </summary>
        public required int ChildID;

        /// <summary>
        /// The resource identifier
        /// </summary>
        public required int MouldID;

        /// <summary>
        /// The product identifier
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The required level
        /// </summary>
        public required int RequiredLevel;

        /// <summary>
        /// The experience
        /// </summary>
        public required double Experience;
    }
}