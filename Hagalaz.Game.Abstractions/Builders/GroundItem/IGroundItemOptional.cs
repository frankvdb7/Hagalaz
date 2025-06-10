using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemOptional : IGroundItemBuild
    {
        /// <summary>
        /// Sets the owner of the ground item.
        /// </summary>
        /// <param name="owner">The character who owns the ground item.</param>
        /// <returns>The current instance of <see cref="IGroundItemOptional"/> for method chaining.</returns>
        IGroundItemOptional WithOwner(ICharacter owner);
        /// <summary>
        /// Specifies the number of ticks after which the ground item will respawn.
        /// </summary>
        /// <param name="respawnTicks">The number of ticks for the item to respawn.</param>
        /// <returns>The current instance of <see cref="IGroundItemOptional"/> for method chaining.</returns>
        IGroundItemOptional WithRespawnTicks(int respawnTicks);
        /// <summary>
        /// Specifies the number of ticks after which the ground item will despawn.
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        IGroundItemOptional WithTicks(int ticks);

        /// <summary>
        /// Marks the ground item as respawning.
        /// </summary>
        /// <returns></returns>
        IGroundItemOptional AsRespawning();
    }
}