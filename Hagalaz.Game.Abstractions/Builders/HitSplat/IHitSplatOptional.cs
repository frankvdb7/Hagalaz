using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Represents a step in the fluent builder pattern for creating a hitsplat where optional
    /// parameters like delay and sender can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHitSplatBuilder"/>.
    /// It inherits from <see cref="IHitSplatSprite"/> to allow chaining multiple hitsplats and from <see cref="IHitSplatBuild"/>
    /// to allow the build process to be finalized.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHitSplatOptional : IHitSplatSprite, IHitSplatBuild
    {
        /// <summary>
        /// Sets the delay before the hitsplat is displayed.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatOptional WithDelay(int delay);

        /// <summary>
        /// Specifies the entity that is the source of the damage or effect represented by the hitsplat.
        /// </summary>
        /// <param name="sender">The <see cref="IRuneObject"/> that initiated the hit.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHitSplatOptional FromSender(IRuneObject sender);
    }
}