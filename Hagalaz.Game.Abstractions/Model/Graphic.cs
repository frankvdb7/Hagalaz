using System;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a request to display a temporary, non-interactive graphical effect at a specific location, such as a spell's impact explosion.
    /// </summary>
    public readonly struct Graphic : IGraphic, IEquatable<Graphic>
    {
        /// <summary>
        /// Gets the unique identifier for the graphical effect.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the delay in game ticks before the graphic is displayed.
        /// </summary>
        public int Delay { get; }

        /// <summary>
        /// Gets the height offset at which the graphic is rendered relative to the ground.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the rotation of the graphic.
        /// </summary>
        public int Rotation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Graphic"/> struct.
        /// </summary>
        /// <param name="id">The unique identifier for the graphic.</param>
        /// <param name="delay">The delay in game ticks before the graphic is displayed.</param>
        /// <param name="height">The height offset for the graphic.</param>
        /// <param name="rotation">The rotation of the graphic.</param>
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