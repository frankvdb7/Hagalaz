using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an animation where optional
    /// parameters like delay and priority can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended to be implemented directly. It is part of the fluent API provided by <see cref="IAnimationBuilder"/>.
    /// It also inherits from <see cref="IAnimationBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAnimationOptional : IAnimationBuild
    {
        /// <summary>
        /// Sets the delay for the animation before it starts playing.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the animation.</returns>
        IAnimationOptional WithDelay(int delay);

        /// <summary>
        /// Sets the priority for the animation. Higher priority animations may override lower priority ones.
        /// </summary>
        /// <param name="priority">The priority level of the animation.</param>
        /// <returns>The same builder instance to allow for further optional configuration or to build the animation.</returns>
        IAnimationOptional WithPriority(int priority);
    }
}