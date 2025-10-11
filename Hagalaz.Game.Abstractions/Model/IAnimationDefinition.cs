using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines a marker interface for an animation definition, which represents the static data for an animation loaded from the cache.
    /// It inherits from <see cref="IAnimationType"/>, which provides the core properties for an animation.
    /// </summary>
    public interface IAnimationDefinition : IAnimationType
    {
        
    }
}