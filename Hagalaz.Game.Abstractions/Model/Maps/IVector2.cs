namespace Hagalaz.Game.Abstractions.Model.Maps
{
    public interface IVector2
    {
        /// <summary>
        /// The x coordinate of this vector.
        /// </summary>
        public int X { get; }
        /// <summary>
        /// The y coordinate of this vector.
        /// </summary>
        public int Y { get; }
    }
}