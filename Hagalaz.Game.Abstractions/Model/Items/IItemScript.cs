using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for a script that controls an item's general behavior and interactions.
    /// </summary>
    public interface IItemScript
    {
        /// <summary>
        /// Checks if a character can trade a specific item.
        /// </summary>
        /// <param name="item">The item to be traded.</param>
        /// <param name="character">The character attempting to trade the item.</param>
        /// <returns><c>true</c> if the item can be traded; otherwise, <c>false</c>.</returns>
        bool CanTradeItem(IItem item, ICharacter character);

        /// <summary>
        /// Checks if a specific item can be sold to a shop.
        /// </summary>
        /// <param name="item">The item to be sold.</param>
        /// <param name="character">The character selling the item.</param>
        /// <returns><c>true</c> if the item can be sold; otherwise, <c>false</c>.</returns>
        bool CanSellItem(IItem item, ICharacter character);

        /// <summary>
        /// Checks if a specific item can be bought from a shop.
        /// </summary>
        /// <param name="item">The item to be bought.</param>
        /// <param name="character">The character buying the item.</param>
        /// <returns><c>true</c> if the item can be bought; otherwise, <c>false</c>.</returns>
        bool CanBuyItem(IItem item, ICharacter character);

        /// <summary>
        /// Determines whether a ground item should be rendered for a specific character.
        /// </summary>
        /// <param name="item">The ground item.</param>
        /// <param name="character">The character viewing the item.</param>
        /// <returns><c>true</c> if the item should be rendered; otherwise, <c>false</c>.</returns>
        bool CanDrawFor(IGroundItem item, ICharacter character);

        /// <summary>
        /// Checks if a specific item can be stacked with another item.
        /// </summary>
        /// <param name="item">The first item.</param>
        /// <param name="otherItem">The second item.</param>
        /// <param name="containerAlwaysStack">A value indicating if the container forces stacking.</param>
        /// <returns><c>true</c> if the items can be stacked; otherwise, <c>false</c>.</returns>
        bool CanStackItem(IItem item, IItem otherItem, bool containerAlwaysStack);

        /// <summary>
        /// A callback executed when this ground item is deleted from a specific character's view.
        /// </summary>
        /// <param name="item">The ground item.</param>
        /// <param name="character">The character for whom the item was deleted.</param>
        void OnDeletedFor(IGroundItem item, ICharacter character);

        /// <summary>
        /// A callback executed when this ground item is rendered for a specific character.
        /// </summary>
        /// <param name="item">The ground item.</param>
        /// <param name="character">The character for whom the item was rendered.</param>
        void OnRenderedFor(IGroundItem item, ICharacter character);

        /// <summary>
        /// A callback executed when a character clicks on this item on the ground.
        /// </summary>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="item">The ground item that was clicked.</param>
        /// <param name="forceRun">A value indicating whether the character should force-run to the item.</param>
        /// <param name="character">The character who clicked the item.</param>
        void ItemClickedOnGround(GroundItemClickType clickType, IGroundItem item, bool forceRun, ICharacter character);

        /// <summary>
        /// A callback executed when a character clicks on this item in their equipment.
        /// </summary>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="item">The item that was clicked.</param>
        /// <param name="character">The character who clicked the item.</param>
        void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character);

        /// <summary>
        /// Handles the logic for dropping this item from a character's inventory.
        /// </summary>
        /// <param name="item">The item to be dropped.</param>
        /// <param name="character">The character dropping the item.</param>
        /// <returns><c>true</c> if the drop was successful; otherwise, <c>false</c>.</returns>
        bool DropItem(IItem item, ICharacter character);

        /// <summary>
        /// A callback executed when a character clicks on this item in their inventory.
        /// </summary>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="item">The item that was clicked.</param>
        /// <param name="character">The character who clicked the item.</param>
        void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character);

        /// <summary>
        /// Handles the "Use item on object" action.
        /// </summary>
        /// <param name="used">The item being used.</param>
        /// <param name="usedOn">The game object the item is being used on.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character);

        /// <summary>
        /// Handles the "Use item on NPC" action.
        /// </summary>
        /// <param name="used">The item being used.</param>
        /// <param name="usedOn">The NPC the item is being used on.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnNpc(IItem used, INpc usedOn, ICharacter character);

        /// <summary>
        /// Handles the "Use item on ground item" action.
        /// </summary>
        /// <param name="used">The item being used.</param>
        /// <param name="usedOn">The ground item the item is being used on.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnGroundItem(IItem used, IGroundItem usedOn, ICharacter character);

        /// <summary>
        /// Handles the "Use item on item" action.
        /// </summary>
        /// <param name="used">The first item.</param>
        /// <param name="usedWith">The second item.</param>
        /// <param name="character">The character using the items.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character);

        /// <summary>
        /// Handles the "Use item on player" action.
        /// </summary>
        /// <param name="used">The item being used.</param>
        /// <param name="usedOn">The character the item is being used on.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnCharacter(IItem used, ICharacter usedOn, ICharacter character);

        /// <summary>
        /// Gets the "Examine" text for this item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <returns>The examine text.</returns>
        string GetExamine(IItem item);
    }
}