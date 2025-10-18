using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for an item that is lying on the ground in the game world.
    /// </summary>
    public interface IGroundItem : IEntity
    {
        /// <summary>
        /// Gets or sets the character who owns this ground item. A null value indicates the item is public.
        /// </summary>
        ICharacter? Owner { get; set; }

        /// <summary>
        /// Gets the item instance that is on the ground.
        /// </summary>
        IItem ItemOnGround { get; }

        /// <summary>
        /// Gets a value indicating whether the item is public and can be seen and taken by any player.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Owner))]
        bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether this ground item will respawn after being taken.
        /// </summary>
        bool IsRespawning { get; }

        /// <summary>
        /// Gets the number of game ticks until this item respawns after being taken.
        /// </summary>
        int RespawnTicks { get; }

        /// <summary>
        /// Gets or sets the number of game ticks remaining until an owned item becomes public or disappears.
        /// </summary>
        int TicksLeft { get; set; }

        /// <summary>
        /// Determines whether this ground item can be stacked with another ground item.
        /// </summary>
        /// <param name="item">The other ground item to check.</param>
        /// <returns><c>true</c> if the items can be stacked; otherwise, <c>false</c>.</returns>
        bool CanStack(IGroundItem item);

        /// <summary>
        /// Determines whether this ground item is set to respawn.
        /// </summary>
        /// <returns><c>true</c> if the item will respawn; otherwise, <c>false</c>.</returns>
        bool CanRespawn();

        /// <summary>
        /// Removes the ground item from the game world.
        /// </summary>
        /// <returns><c>true</c> if the item was successfully despawned; otherwise, <c>false</c>.</returns>
        bool Despawn();

        /// <summary>
        /// Creates a new, identical copy of this ground item.
        /// </summary>
        /// <returns>A new <see cref="IGroundItem"/> instance.</returns>
        IGroundItem Clone();
    }
}