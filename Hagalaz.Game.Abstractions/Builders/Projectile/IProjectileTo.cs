using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.Projectile
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a projectile where the destination
    /// of the projectile must be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IProjectileBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IProjectileTo
    {
        /// <summary>
        /// Sets the destination of the projectile to a specific creature.
        /// </summary>
        /// <param name="creature">The target <see cref="ICreature"/> for the projectile.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's duration.</returns>
        IProjectileDuration ToCreature(ICreature creature);

        /// <summary>
        /// Sets the destination of the projectile to a specific game object.
        /// </summary>
        /// <param name="gameObject">The target <see cref="IGameObject"/> for the projectile.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's duration.</returns>
        IProjectileDuration ToGameObject(IGameObject gameObject);

        /// <summary>
        /// Sets the destination of the projectile to a specific ground item.
        /// </summary>
        /// <param name="groundItem">The target <see cref="IGroundItem"/> for the projectile.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's duration.</returns>
        IProjectileDuration ToGroundItem(IGroundItem groundItem);

        /// <summary>
        /// Sets the destination of the projectile to a specific location coordinate.
        /// </summary>
        /// <param name="location">The target <see cref="ILocation"/> for the projectile.</param>
        /// <returns>The next step in the fluent builder chain, which requires specifying the projectile's duration.</returns>
        IProjectileDuration ToLocation(ILocation location);
    }
}