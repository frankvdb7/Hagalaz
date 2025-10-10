using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a projectile where the travel duration must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// The duration is typically calculated based on the distance between the start and end points, but can be set manually.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileDuration
    {
        /// <summary>
        /// Sets the duration of the projectile's travel time.
        /// </summary>
        /// <param name="duration">The duration of the projectile's flight, in game ticks.</param>
        /// <returns>The next step in the fluent builder chain, allowing for optional parameters to be set.</returns>
        public IProjectileOptional WithDuration(int duration);
    }
}