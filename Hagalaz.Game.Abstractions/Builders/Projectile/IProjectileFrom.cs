using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a projectile where the origin
    /// (or source) of the projectile must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileFrom
    {
        /// <summary>
        /// Sets the origin of the projectile to a specific creature.
        /// </summary>
        /// <param name="creature">The <see cref="ICreature"/> from which the projectile originates.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's destination.</returns>
        IProjectileTo FromCreature(ICreature creature);

        /// <summary>
        /// Sets the origin of the projectile to a specific game object.
        /// </summary>
        /// <param name="gameObject">The <see cref="IGameObject"/> from which the projectile originates.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's destination.</returns>
        IProjectileTo FromGameObject(IGameObject gameObject);

        /// <summary>
        /// Sets the origin of the projectile to a specific ground item.
        /// </summary>
        /// <param name="groundItem">The <see cref="IGroundItem"/> from which the projectile originates.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's destination.</returns>
        IProjectileTo FromGroundItem(IGroundItem groundItem);

        /// <summary>
        /// Sets the origin of the projectile to a specific location coordinate.
        /// </summary>
        /// <param name="location">The <see cref="ILocation"/> from which the projectile originates.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's destination.</returns>
        IProjectileTo FromLocation(ILocation location);
    }
}