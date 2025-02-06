using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemScript
    {
        /// <summary>
        /// Get's if specific character can trade specific item.
        /// By default this method does check item definition if it's tradeable.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which is going to trade specific item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can trade item] the specified item; otherwise, <c>false</c>.
        /// </returns>
        bool CanTradeItem(IItem item, ICharacter character);
        /// <summary>
        /// Get's if specific item can be sold to the specified shop.
        /// </summary>
        /// <param name="shop">The shop.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool CanSellItem(IItem item, ICharacter character);

        /// <summary>
        /// Get's if specific item can be bought from the specified shop.
        /// </summary>
        /// <param name="shop">The shop.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool CanBuyItem(IItem item, ICharacter character);
        /// <summary>
        /// Determines whether this instance [can render for] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can render for] the specified item; otherwise, <c>false</c>.
        /// </returns>
        bool CanDrawFor(IGroundItem item, ICharacter character);
        /// <summary>
        /// Get's if specific item can be stacked with other item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="otherItem">The other item.</param>
        /// <param name="containerAlwaysStack">if set to <c>true</c> [container always stack].</param>
        /// <returns>
        ///   <c>true</c> if this instance can stack the specified other item; otherwise, <c>false</c>.
        /// </returns>
        bool CanStackItem(IItem item, IItem otherItem, bool containerAlwaysStack);
        /// <summary>
        /// Happens when this item is deleted for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        void OnDeletedFor(IGroundItem item, ICharacter character);
        /// <summary>
        /// Happens when this item is rendered for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        void OnRenderedFor(IGroundItem item, ICharacter character);
        /// <summary>
        /// Happens when specific item is clicked on the ground.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item on the ground.</param>
        /// <param name="forceRun">Wheter player should run while walking to item location.</param>
        /// <param name="character">Character which clicked on the item.</param>
        void ItemClickedOnGround(GroundItemClickType clickType, IGroundItem item, bool forceRun, ICharacter character);
        /// <summary>
        /// Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character);
        /// <summary>
        /// Get's called when specific item is about to be droped.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which should drop the item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
         bool DropItem(IItem item, ICharacter character);
        /// <summary>
        /// Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character);
        /// <summary>
        /// Uses the item on game object.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character);
        /// <summary>
        /// Uses the item on NPC.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool UseItemOnNpc(IItem used, INpc usedOn, ICharacter character);
        /// <summary>
        /// Uses the item on a grounditem.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        bool UseItemOnGroundItem(IItem used, IGroundItem usedOn, ICharacter character);
        /// <summary>
        /// Uses the item on an other item.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character);
        /// <summary>
        /// Uses the item on a character.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        bool UseItemOnCharacter(IItem used, ICharacter usedOn, ICharacter character);
        /// <summary>
        /// Gets the examine.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        string GetExamine(IItem item);
    }
}
