using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages animation definitions.
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>
        /// Finds an animation definition by its unique identifier.
        /// </summary>
        /// <param name="animationId">The ID of the animation to find.</param>
        /// <returns>The <see cref="IAnimationDefinition"/> if found; otherwise, <c>null</c>.</returns>
        IAnimationDefinition FindAnimationById(int animationId);
    }
}