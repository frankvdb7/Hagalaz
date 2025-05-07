using System.Linq;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Dialogues.Items;

namespace Hagalaz.Game.Scripts.Model.Items
{
    /// <summary>
    /// Abstract item script class.
    /// </summary>
    public abstract class ItemScript : IItemScript
    {
        /// <summary>
        /// Get's if specific item can be deposited to bank.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">The character.</param>
        /// <returns><c>true</c> if this instance [can deposit item] the specified item; otherwise, <c>false</c>.</returns>
        public virtual bool CanDepositItem(IItem item, ICharacter character) => true;

        /// <summary>
        /// Get's if specific item can be sold to the specified shop.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool CanSellItem(IItem item, ICharacter character) => CanTradeItem(item, character) && new SellAllowEvent(character, item).Send();

        /// <summary>
        /// Get's if specific item can be bought from the specified shop.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool CanBuyItem(IItem item, ICharacter character) => new BuyAllowEvent(character, item).Send();

        /// <summary>
        /// Get's if specific character can drop specific item.
        /// By default this method does check item definition if it's dropable and sends DropAllowEvent.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which is going to drop specified item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can drop item] the specified item; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanDropItem(IItem item, ICharacter character) => !item.ItemDefinition.HasDestroyOption && new DropAllowEvent(character, item).Send();

        /// <summary>
        /// Get's if specific character can trade specific item.
        /// By default this method does check item definition if it's tradeable.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which is going to trade specific item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can trade item] the specified item; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanTradeItem(IItem item, ICharacter character)
        {
            if (character.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                return false;
            if (!item.ItemDefinition.Tradeable)
            {
                return false;
            }

            return item.ExtraData.Length == 0;
        }

        /// <summary>
        /// Get's if specific character can take specific item.
        /// </summary>
        /// <param name="item">Item which should be taken.</param>
        /// <param name="character">Character which is taking the item.</param>
        /// <returns><c>true</c> if this instance [can take item] the specified item; otherwise, <c>false</c>.</returns>
        public virtual bool CanTakeItem(IGroundItem item, ICharacter character) => true;

        /// <summary>
        /// Get's if specific item can be stacked with other item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="otherItem">The other item.</param>
        /// <param name="containerAlwaysStack">if set to <c>true</c> [container always stack].</param>
        /// <returns>
        ///   <c>true</c> if this instance can stack the specified other item; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanStackItem(IItem item, IItem otherItem, bool containerAlwaysStack)
        {
            if (item.Id == otherItem.Id && (item.ItemDefinition.Stackable || item.ItemDefinition.Noted || containerAlwaysStack))
            {
                if (item.ExtraData.Length != otherItem.ExtraData.Length)
                    return false;
                return !item.ExtraData.Where((t, i) => t != otherItem.ExtraData[i]).Any();
            }

            return false;
        }

        /// <summary>
        /// Get's called when specific item is about to be dropped.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which should drop the item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public virtual bool DropItem(IItem item, ICharacter character)
        {
            if (CanDropItem(item, character))
                return character.Inventory.DropItem(item);
            if (!item.ItemDefinition.HasDestroyOption)
            {
                return false;
            }

            var destroyDialogue = character.ServiceProvider.GetRequiredService<DestroyItemDialogue>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.DestroyItemBox, 0, destroyDialogue, true, item);
            return false;
        }

        /// <summary>
        /// Get's called when specific item is about to be taken by character.
        /// </summary>
        /// <param name="item">Item on ground.</param>
        /// <param name="character">Character which should take the item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public virtual bool TakeItem(IGroundItem item, ICharacter character)
        {
            if (!CanTakeItem(item, character))
                return false;
            if (!character.Inventory.HasSpaceFor(item.ItemOnGround))
            {
                character.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            if (!item.Despawn())
            {
                character.SendChatMessage(GameStrings.GroundItemGone);
                return false;
            }

            if (!character.Inventory.Add(item.ItemOnGround.Clone()))
                return false;
            return true;
        }

        /// <summary>
        /// Happens when this item is rendered for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public virtual void OnRenderedFor(IGroundItem item, ICharacter character) { }

        /// <summary>
        /// Happens when this item is deleted for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public virtual void OnDeletedFor(IGroundItem item, ICharacter character) { }

        /// <summary>
        /// Determines whether this instance [can render for] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can render for] the specified item; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanDrawFor(IGroundItem item, ICharacter character)
        {
            if (item.IsRespawning)
                return false;
            return item.Owner == null || item.Owner == character;
        }

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
        public virtual bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character) => false;

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
        public virtual bool UseItemOnGroundItem(IItem used, IGroundItem usedOn, ICharacter character) => false;

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
        public virtual bool UseItemOnCharacter(IItem used, ICharacter usedOn, ICharacter character) => false;

        /// <summary>
        /// Uses the item on NPC.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool UseItemOnNpc(IItem used, INpc usedOn, ICharacter character) => false;

        /// <summary>
        /// Uses the item on game object.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character) => false;

        /// <summary>
        /// Itemclickeds the on ground walk.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="canInteract">if set to <c>true</c> [can interact].</param>
        public virtual void ItemClickedOnGroundReached(IGroundItem item, ICharacter character, GroundItemClickType clickType, bool canInteract)
        {
            if (canInteract)
                ItemClickedOnGroundPerform(item, clickType, character);
            else
            {
                if (character.HasState(StateType.Frozen))
                    character.SendChatMessage(GameStrings.MagicalForceMovement);
                else
                    character.SendChatMessage(GameStrings.YouCantReachThat);
            }
        }

        /// <summary>
        /// Happens when specific item is clicked on the ground.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item on the ground.</param>
        /// <param name="forceRun">Wheter player should run while walking to item location.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public virtual void ItemClickedOnGround(GroundItemClickType clickType, IGroundItem item, bool forceRun, ICharacter character)
        {
            if (new WalkAllowEvent(character, item.Location, forceRun, false).Send())
            {
                character.Interrupt(this);
                character.Movement.MovementType = character.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                character.QueueTask(new GroundItemReachTask(character, item, (success) => ItemClickedOnGroundReached(item, character, clickType, success)));
            }
        }


        /// <summary>
        /// This method get's called when character clicks ground item and then
        /// reaches it.
        /// </summary>
        /// <param name="item">Item which was clicked.</param>
        /// <param name="clickType">Type of the item click.</param>
        /// <param name="character">Character which clicked the item.</param>
        public virtual void ItemClickedOnGroundPerform(IGroundItem item, GroundItemClickType clickType, ICharacter character)
        {
            if (clickType == GroundItemClickType.Option3Click)
                TakeItem(item, character);
            else if (clickType == GroundItemClickType.Option6Click)
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("gitem[id=" + item.ItemOnGround.Id + ",loc=(" + item.Location.X + "," + item.Location.Y + "," + item.Location.Z + ")", ChatMessageType.ConsoleText);
                character.SendChatMessage(GetExamine(item.ItemOnGround));
            }
            else
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("ground_item_click[id=" + item.ItemOnGround.Id + ",type=" + clickType + "]", ChatMessageType.ConsoleText);
                else
                    character.SendChatMessage(GameStrings.NothingInterestingHappens);
            }
        }


        /// <summary>
        /// Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public virtual void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            character.Interrupt(this);
            if (clickType == ComponentClickType.Option2Click)
                item.EquipmentScript.EquipItem(item, character);
            else if (clickType == ComponentClickType.Option8Click)
                DropItem(item, character);
            else if (clickType == ComponentClickType.Option10Click)
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("item[id=" + item.Id + "]", ChatMessageType.ConsoleText);
                character.SendChatMessage(GetExamine(item));
            }
            else
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("item_inventory_click[id=" + item.Id + ",type=" + clickType + "]", ChatMessageType.ConsoleText);
                character.SendChatMessage(GameStrings.NothingInterestingHappens);
            }
        }

        /// <summary>
        /// Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public virtual void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            character.Interrupt(this);
            if (clickType == ComponentClickType.LeftClick)
                item.EquipmentScript.UnEquipItem(item, character);
            else if (clickType == ComponentClickType.Option2Click)
                item.EquipmentScript.UnEquipItem(item, character);
            else if (clickType == ComponentClickType.Option10Click)
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("item_equipment[id=" + item.Id + "]", ChatMessageType.ConsoleText);
                character.SendChatMessage(GetExamine(item));
            }
            else
            {
                if (character.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    character.SendChatMessage("item_equipment_click[id=" + item.Id + ",type=" + clickType + "]");
                else
                    character.SendChatMessage(GameStrings.NothingInterestingHappens);
            }
        }

        /// <summary>
        /// Gets the examine.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public virtual string GetExamine(IItem item) => item.ItemDefinition.Examine;
    }
}