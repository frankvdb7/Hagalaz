namespace Hagalaz.Game.Abstractions.Model.Maps
{
    public interface IVector3 : IVector2
    {
        /// <summary>
        /// The z coordinate of this vector.
        /// </summary>
        public int Z { get; }
    }
}
