using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAnimationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="animationId"></param>
        /// <returns></returns>
        IAnimationDefinition FindAnimationById(int animationId);
    }
}