using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGroundItem : IEntity
    {
        /// <summary>
        /// Contains owner character of this ground item.
        /// Can be null.
        /// </summary>
        /// <value>The owner.</value>
        ICharacter? Owner { get; set; }
        /// <summary>
        /// Gets the item on ground.
        /// </summary>
        /// <value>
        /// The item on ground.
        /// </value>
        IItem ItemOnGround { get; }
        /// <summary>
        /// Contains a bool wether the item is public (no owner)
        /// </summary>
        [MemberNotNullWhen(false, nameof(Owner))]
        bool IsPublic { get; }
        /// <summary>
        /// Contains a bool wether to respawn this instance.
        /// </summary>
        /// <value>Should respawn.</value>
        bool IsRespawning { get; }
        /// <summary>
        /// Gets the amount of ticks before this item respawns.
        /// </summary>
        int RespawnTicks { get; }
        /// <summary>
        /// Contains amount of owner ticks are left before the item
        /// is either made public or destroyed.
        /// </summary>
        /// <value>The owner ticks left.</value>
        int TicksLeft { get; set; }
        /// <summary>
        /// Determines whether this instance can stack the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if this instance can stack the specified item; otherwise, <c>false</c>.
        /// </returns>
        bool CanStack(IGroundItem item);
        /// <summary>
        /// Determines whether this instance can respawn.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can respawn; otherwise, <c>false</c>.
        /// </returns>
        bool CanRespawn();
        /// <summary>
        /// Despawns this instance.
        /// </summary>
        /// <returns></returns>
        bool Despawn();
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        IGroundItem Clone();
    }
}
