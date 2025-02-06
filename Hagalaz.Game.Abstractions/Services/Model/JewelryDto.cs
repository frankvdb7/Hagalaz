namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public partial class JewelryDto
    {
        /// <summary>
        /// The child identifier
        /// </summary>
        public int ChildID;

        /// <summary>
        /// The type
        /// </summary>
        public JewelryType Type;

        /// <summary>
        /// The resource identifier
        /// </summary>
        public int ResourceID;

        /// <summary>
        /// The product identifier
        /// </summary>
        public int ProductID;

        /// <summary>
        /// The required level
        /// </summary>
        public int RequiredLevel;

        /// <summary>
        /// The experience
        /// </summary>
        public double Experience;
    }
}