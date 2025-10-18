using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for a provider that determines how a hitsplat should be rendered on the client.
    /// This allows for different visual styles of hitsplats based on their type and properties.
    /// </summary>
    public interface IHitSplatRenderTypeProvider
    {
        /// <summary>
        /// Gets the rendering type for a given hitsplat configuration.
        /// </summary>
        /// <param name="splatType">The type of the hitsplat (e.g., Melee, Poison, Heal).</param>
        /// <param name="active">A value indicating whether the hitsplat represents active damage or is for display purposes only.</param>
        /// <param name="isCriticalDamage">A value indicating whether the hit was a critical or max hit, which may have a different visual.</param>
        /// <returns>The <see cref="HitSplatRenderType"/> that defines how the client should display the hitsplat.</returns>
        /// <exception cref="System.Exception">Throws when an unknown or unhandled splat type is provided.</exception>
        HitSplatRenderType GetRenderType(HitSplatType splatType, bool active, bool isCriticalDamage);
    }
}