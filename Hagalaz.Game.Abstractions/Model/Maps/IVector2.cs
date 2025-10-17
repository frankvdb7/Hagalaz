namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a 2D integer vector.
    /// </summary>
    public interface IVector2
    {
        /// <summary>
        /// Gets the X-component of the vector.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y-component of the vector.
        /// </summary>
        public int Y { get; }
    }
}