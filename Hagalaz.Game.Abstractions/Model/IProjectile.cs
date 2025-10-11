using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a projectile, which is a moving graphic that travels from a start point to an end point.
    /// </summary>
    public interface IProjectile
    {
        /// <summary>
        /// Gets the starting location of the projectile.
        /// </summary>
        ILocation FromLocation { get; }

        /// <summary>
        /// Gets the destination location of the projectile.
        /// </summary>
        ILocation ToLocation { get; }

        /// <summary>
        /// Gets the creature that fired the projectile, if any.
        /// </summary>
        ICreature? FromCreature { get; }

        /// <summary>
        /// Gets the creature targeted by the projectile, if any.
        /// </summary>
        ICreature? ToCreature { get; }

        /// <summary>
        /// Gets a value indicating whether the projectile's starting height should be adjusted based on the source creature's flying height.
        /// </summary>
        bool AdjustFromFlyingHeight { get; }

        /// <summary>
        /// Gets a value indicating whether the projectile's ending height should be adjusted based on the target creature's flying height.
        /// </summary>
        bool AdjustToFlyingHeight { get; }

        /// <summary>
        /// Gets the ID of the body part from which the projectile originates, used for height adjustments.
        /// </summary>
        int FromBodyPartId { get; }

        /// <summary>
        /// Gets the unique identifier for the projectile's visual graphic.
        /// </summary>
        int GraphicId { get; }

        /// <summary>
        /// Gets the delay in game ticks before the projectile is launched.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Gets the total duration of the projectile's flight in game ticks.
        /// </summary>
        int Duration { get; }

        /// <summary>
        /// Gets the starting height of the projectile, relative to its location.
        /// </summary>
        int FromHeight { get; }

        /// <summary>
        /// Gets the ending height of the projectile, relative to its location.
        /// </summary>
        int ToHeight { get; }

        /// <summary>
        /// Gets the slope or arc of the projectile's flight path.
        /// </summary>
        int Slope { get; }

        /// <summary>
        /// Gets the initial angle of the projectile's launch.
        /// </summary>
        int Angle { get; }
    }
}