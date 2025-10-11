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
        /// Returns an animation that will reset the current animation if queued
        /// </summary>
        public static readonly Animation Reset = new(-1, 0, 0);

        /// <summary>
        /// Creates a new animation display.
        /// </summary>
        /// <param name="id">The animation id.</param>
        /// <param name="delay">The delay of the animation display.</param>
        /// <param name="priority">The priority of the animation. If multiple animations are queued, the highest priority is played.</param>
        /// <returns>Returns a new instance holding the animation data.</returns>
        public static Animation Create(int id, int delay = 0, int priority = 0)
        {
            if (id == -1 && delay == 0 && priority == 0)
            {
                return Reset;
            }
            return new Animation(id, delay, priority);
        }

        /// <summary>
        /// Get's if this animation equals to other animation.
        /// </summary>
        /// <returns></returns>
        public bool Equals(Animation animation) => Id == animation.Id && Delay == animation.Delay;

        public override bool Equals(object? obj) => obj is Animation animation && Equals(animation);

        public override int GetHashCode() => HashCode.Combine(Id, Delay, Priority);

        public static bool operator ==(Animation left, Animation right) => left.Equals(right);

        public static bool operator !=(Animation left, Animation right) => !(left == right);
    }
}