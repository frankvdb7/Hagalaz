using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a ground item where optional
    /// parameters like owner, respawn behavior, and despawn timers can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IGroundItemBuilder"/>.
    /// It also inherits from <see cref="IGroundItemBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemOptional : IGroundItemBuild
    {
        /// <summary>
        /// Sets the owner of the ground item, making it visible only to that character for a certain period.
        /// </summary>
        /// <param name="owner">The character who owns the ground item.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGroundItemOptional WithOwner(ICharacter owner);
        /// <summary>
        /// Specifies the number of game ticks after which the ground item will respawn after being picked up.
        /// This is typically used for static, respawning items in the world.
        /// </summary>
        /// <param name="respawnTicks">The number of ticks before the item respawns.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGroundItemOptional WithRespawnTicks(int respawnTicks);
        /// <summary>
        /// Specifies the number of game ticks after which the ground item will despawn and be removed from the world.
        /// </summary>
        /// <param name="ticks">The number of ticks until the item despawns.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGroundItemOptional WithTicks(int ticks);

        /// <summary>
        /// Marks the ground item as a respawning item. This is a shorthand for setting respawn behavior.
        /// </summary>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IGroundItemOptional AsRespawning();
    }
}