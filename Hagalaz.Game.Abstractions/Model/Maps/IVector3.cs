namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a 3D integer vector.
    /// </summary>
    public interface IVector3 : IVector2
    {
        /// <summary>
        /// Gets the Z-component of the vector.
        /// </summary>
        public int Z { get; }
    }
}