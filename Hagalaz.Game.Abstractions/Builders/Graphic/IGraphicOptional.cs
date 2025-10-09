using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a graphic effect where optional
    /// parameters like delay, height, and rotation can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGraphicBuilder"/>.
    /// It also inherits from <see cref="IGraphicBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphicOptional : IGraphicBuild
    {
        /// <summary>
        /// Sets the delay before the graphic effect is displayed.
        /// </summary>
        /// <param name="delay">The delay in game ticks.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGraphicOptional WithDelay(int delay);

        /// <summary>
        /// Sets the height at which the graphic effect is rendered relative to the ground.
        /// </summary>
        /// <param name="height">The height offset for the graphic.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGraphicOptional WithHeight(int height);

        /// <summary>
        /// Sets the rotation of the graphic effect.
        /// </summary>
        /// <param name="rotation">The rotation value for the graphic.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGraphicOptional WithRotation(int rotation);
    }
}