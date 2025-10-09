namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the contract for an animation type, which contains information about a single animation sequence used in the game.
    /// </summary>
    public interface IAnimationType : IType
    {
        /// <summary>
        /// Gets the unique identifier for this animation type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the priority level of the animation. Higher priority animations can override lower priority ones.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// A method that is called after the animation data has been decoded from the cache.
        /// This can be used for post-processing, validation, or linking related data.
        /// </summary>
        void AfterDecode();
    }
}