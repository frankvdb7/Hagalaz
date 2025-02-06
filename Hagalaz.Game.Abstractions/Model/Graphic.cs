using System;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a single graphic display request.
    /// </summary>
    public readonly struct Graphic : IGraphic, IEquatable<Graphic>
    {
        /// <summary>
        /// Gets the graphic id (id from client).
        /// </summary>
        /// <value>The id.</value>
        public int Id { get; }

        /// <summary>
        /// Gets the graphic delay.
        /// </summary>
        /// <value>The delay.</value>
        public int Delay { get; }

        /// <summary>
        /// Get's the graphic height.
        /// </summary>
        /// <value>The height.</value>
        public int Height { get; }

        /// <summary>
        /// Get's the graphic rotation.
        /// </summary>
        /// <value>The rotation.</value>
        public int Rotation { get; }

        /// <summary>
        /// Constructs a new graphic.
        /// </summary>
        /// <param name="id">The graphic id.</param>
        /// <param name="delay">The delay of the graphic display.</param>
        /// <param name="height">The height.</param>
        /// <param name="rotation">The rotation.</param>
        public Graphic(int id, int delay, int height, int rotation)
        {
            Id = id;
            Delay = delay;
            Height = height;
            Rotation = rotation;
        }

        /// <summary>
        /// Creates a new graphic display.
        /// </summary>
        /// <param name="id">The graphic id.</param>
        /// <param name="delay">The delay of the graphic display.</param>
        /// <param name="height">The height.</param>
        /// <param name="rotation">The rotation.</param>
        /// <returns>Returns a new instance holding the graphic data.</returns>
        public static IGraphic Create(int id, int delay = 0, int height = 0, int rotation = 0) => new Graphic(id, delay, height, rotation);

        /// <summary>
        /// Get's if this graphic equals to other given graphic.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Equals(Graphic other) => Id == other.Id && Delay == other.Delay && Height == other.Height && Rotation == other.Rotation;

        public override int GetHashCode() => HashCode.Combine(Id, Delay, Height, Rotation);

        public override bool Equals(object? obj) => obj is Graphic graphic && Equals(graphic);

        public static bool operator ==(Graphic left, Graphic right) => left.Equals(right);

        public static bool operator !=(Graphic left, Graphic right) => !(left == right);
    }
}