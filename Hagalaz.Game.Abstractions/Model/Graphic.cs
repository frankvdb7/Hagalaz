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
        /// Creates a new <see cref="Graphic"/> instance.
        /// </summary>
        /// <param name="id">The unique identifier for the graphic.</param>
        /// <param name="delay">The delay in game ticks before the graphic is displayed. Defaults to 0.</param>
        /// <param name="height">The height offset for the graphic. Defaults to 0.</param>
        /// <param name="rotation">The rotation of the graphic. Defaults to 0.</param>
        /// <returns>A new <see cref="IGraphic"/> instance with the specified data.</returns>
        public static IGraphic Create(int id, int delay = 0, int height = 0, int rotation = 0) => new Graphic(id, delay, height, rotation);

        /// <summary>
        /// Determines whether this graphic is equal to another graphic, based on their Id, Delay, Height, and Rotation.
        /// </summary>
        /// <param name="other">The graphic to compare with this one.</param>
        /// <returns><c>true</c> if the graphics are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Graphic other) => Id == other.Id && Delay == other.Delay && Height == other.Height && Rotation == other.Rotation;

        /// <summary>
        /// Gets a hash code for the graphic, combining its Id, Delay, Height, and Rotation.
        /// </summary>
        /// <returns>An integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(Id, Delay, Height, Rotation);

        /// <summary>
        /// Determines whether the specified object is equal to the current graphic.
        /// </summary>
        /// <param name="obj">The object to compare with the current graphic.</param>
        /// <returns><c>true</c> if the specified object is equal to the current graphic; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is Graphic graphic && Equals(graphic);

        /// <summary>
        /// Compares two <see cref="Graphic"/> instances for equality.
        /// </summary>
        /// <param name="left">The first graphic to compare.</param>
        /// <param name="right">The second graphic to compare.</param>
        /// <returns><c>true</c> if the graphics are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Graphic left, Graphic right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="Graphic"/> instances for inequality.
        /// </summary>
        /// <param name="left">The first graphic to compare.</param>
        /// <param name="right">The second graphic to compare.</param>
        /// <returns><c>true</c> if the graphics are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Graphic left, Graphic right) => !(left == right);
    }
}