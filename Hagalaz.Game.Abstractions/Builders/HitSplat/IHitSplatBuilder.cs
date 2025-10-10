namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    /// <summary>
    /// Defines the contract for a hitsplat builder, which serves as the entry point
    /// for constructing an <see cref="Model.Combat.IHitSplat"/> object using a fluent interface.
    /// </summary>
    public interface IHitSplatBuilder
    {
        /// <summary>
        /// Begins the process of building a new hitsplat.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the hitsplat's visual sprite.</returns>
        IHitSplatSprite Create();
    }
}