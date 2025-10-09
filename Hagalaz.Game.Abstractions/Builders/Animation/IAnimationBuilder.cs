namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    /// <summary>
    /// Defines the contract for an animation builder, which serves as the entry point
    /// for constructing an <see cref="Model.IAnimation"/> object using a fluent interface.
    /// </summary>
    public interface IAnimationBuilder
    {
        /// <summary>
        /// Begins the process of building a new animation.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the animation ID.</returns>
        IAnimationId Create();
    }
}