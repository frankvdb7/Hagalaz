namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition for tanning a hide.
    /// </summary>
    public record TanDto
    {
        /// <summary>
        /// The item ID of the hide to be tanned.
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The item ID of the resulting leather.
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The base cost in coins to tan this hide.
        /// </summary>
        public required int BasePrice;
    }
}