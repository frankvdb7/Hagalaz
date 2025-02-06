namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record TanDto
    {
        /// <summary>
        /// The resource Id
        /// </summary>
        public required int ResourceID;

        /// <summary>
        /// The product Id
        /// </summary>
        public required int ProductID;

        /// <summary>
        /// The base price
        /// </summary>
        public required int BasePrice;
    }
}