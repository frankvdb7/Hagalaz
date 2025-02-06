using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHitSplatRenderTypeProvider
    {
        /// <summary>
        /// Get's hit render type.
        /// </summary>
        /// <param name="splatType">Type of the hit splat.</param>
        /// <param name="active">Whether damage is active.</param>
        /// <param name="isCriticalDamage">Whether hit is maxed.</param>
        /// <returns>HitSplatRenderType.</returns>
        /// <exception cref="System.Exception"></exception>
        HitSplatRenderType GetRenderType(HitSplatType splatType, bool active, bool isCriticalDamage);
    }
}