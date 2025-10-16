using System;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a request to play a single animation sequence, such as a character's attack or an emote.
    /// </summary>
    public readonly struct Animation : IAnimation, IEquatable<Animation>
    {
        /// <summary>
        /// Gets the unique identifier for the animation sequence.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the delay in game ticks before the animation starts playing.
        /// </summary>
        public int Delay { get; }

        /// <summary>
        /// Gets the priority of the animation. Higher priority animations can override lower priority ones.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> struct.
        /// </summary>
        /// <param name="id">The unique identifier for the animation sequence.</param>
        /// <param name="delay">The delay in game ticks before the animation starts.</param>
        /// <param name="priority">The priority level of the animation.</param>
        public Animation(int id, int delay, int priority)
        {
            Id = id;
            Delay = delay;
            Priority = priority;
        }

        /// <summary>
        /// Represents a special animation instance that, when queued, resets the currently playing animation.
        /// </summary>
        public static readonly Animation Reset = new(-1, 0, 0);

        /// <summary>
        /// Creates a new <see cref="Animation"/> instance.
        /// </summary>
        /// <param name="id">The unique identifier for the animation sequence.</param>
        /// <param name="delay">The delay in game ticks before the animation starts. Defaults to 0.</param>
        /// <param name="priority">The priority of the animation. If multiple animations are queued, the one with the highest priority is played. Defaults to 0.</param>
        /// <returns>A new <see cref="Animation"/> instance with the specified data.</returns>
        public static Animation Create(int id, int delay = 0, int priority = 0)
        {
            if (id == -1 && delay == 0 && priority == 0)
            {
                return Reset;
            }
            return new Animation(id, delay, priority);
        }

        /// <summary>
        /// Determines whether this animation is equal to another animation, based on their Id and Delay.
        /// </summary>
        /// <param name="animation">The animation to compare with this one.</param>
        /// <returns><c>true</c> if the animations are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Animation animation) => Id == animation.Id && Delay == animation.Delay;

        /// <summary>
        /// Determines whether the specified object is equal to the current animation.
        /// </summary>
        /// <param name="obj">The object to compare with the current animation.</param>
        /// <returns><c>true</c> if the specified object is equal to the current animation; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is Animation animation && Equals(animation);

        /// <summary>
        /// Gets a hash code for the animation, combining its Id, Delay, and Priority.
        /// </summary>
        /// <returns>An integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(Id, Delay, Priority);

        /// <summary>
        /// Compares two <see cref="Animation"/> instances for equality.
        /// </summary>
        /// <param name="left">The first animation to compare.</param>
        /// <param name="right">The second animation to compare.</param>
        /// <returns><c>true</c> if the animations are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Animation left, Animation right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="Animation"/> instances for inequality.
        /// </summary>
        /// <param name="left">The first animation to compare.</param>
        /// <param name="right">The second animation to compare.</param>
        /// <returns><c>true</c> if the animations are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Animation left, Animation right) => !(left == right);
    }
}