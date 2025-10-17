namespace Hagalaz.Game.Abstractions.Services.Model
{
    public partial class JewelryDto
    {
        /// <summary>
        /// Defines the different types of jewelry that can be crafted.
        /// </summary>
        public enum JewelryType
        {
            /// <summary>
            /// An amulet.
            /// </summary>
            Amulet,
            /// <summary>
            /// A ring.
            /// </summary>
            Ring,
            /// <summary>
            /// A bracelet.
            /// </summary>
            Bracelet,
            /// <summary>
            /// A necklace.
            /// </summary>
            Necklace,
        }
    }
}