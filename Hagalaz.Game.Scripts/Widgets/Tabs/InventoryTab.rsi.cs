using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents inventory tab.
    /// </summary>
    public class InventoryTab : WidgetScript
    {
        /// <summary>
        ///     Contains inventory changes unEquipHandler.
        /// </summary>
        private EventHappened _handler;

        public InventoryTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(0, 0, 27, 4554126);
            InterfaceInstance.SetOptions(0, 28, 55, 2097152);
            InterfaceInstance.AttachClickHandler(0, (component, type, itemID, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var item = Owner.Inventory[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                item.ItemScript.ItemClickedInInventory(type, item, Owner);
                return true;
            });
            InterfaceInstance.AttachDragHandler(0, (fromComponentID, fExtra2, fromSlot, to, toComponentID, tExtra2, toSlot) =>
            {
                if (fromComponentID != 0 || toComponentID != 0)
                {
                    return false;
                }

                if (to != InterfaceInstance)
                {
                    return false;
                }

                if (fromSlot < 0 || fromSlot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                toSlot -= 28;
                if (toSlot < 0 || toSlot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                Owner.Inventory.Swap(fromSlot, toSlot);
                return true;
            });

            InterfaceInstance.AttachUseOnObjectHandler(0, (componentID, usedOn, forceRun, itemId, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var used = Owner.Inventory[slot];
                if (used == null || used.Id != itemId)
                {
                    return false;
                }

                Owner.ForceRunMovementType(forceRun);
                var targetHandle = usedOn.Handle;
                var entityService = Owner.ServiceProvider.GetRequiredService<IEntityService>();
                var task = new GameObjectReachTask(Owner, targetHandle, success =>
                {
                    if (success && entityService.TryResolve<IGameObject>(targetHandle, out var target) && target is not null && !target.IsDisabled)
                    {
                        if (used.ItemScript.UseItemOnGameObject(used, target, Owner))
                        {
                            return;
                        }

                        if (target.Script.UseItemOnGameObject(used, Owner))
                        {
                            return;
                        }

                        if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                        {
                            Owner.SendChatMessage("item_used_on_object[used_id=" + used.Id + ",usedOn=" + target.Id + "]", ChatMessageType.ConsoleText);
                        }
                        else
                        {
                            Owner.SendChatMessage(GameStrings.NothingInterestingHappens);
                        }
                    }
                    else
                    {
                        Owner.SendChatMessage(GameStrings.YouCantReachThat);
                    }
                });
                Owner.QueueTask(task);
                return true;
            });

            InterfaceInstance.AttachUseOnGroundItemHandler(0, (componentID, usedOn, forceRun, itemId, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var used = Owner.Inventory[slot];
                if (used == null || used.Id != itemId)
                {
                    return false;
                }

                Owner.ForceRunMovementType(forceRun);
                var targetHandle = usedOn.Handle;
                var entityService = Owner.ServiceProvider.GetRequiredService<IEntityService>();
                var task = new GroundItemReachTask(Owner, targetHandle, success =>
                {
                    if (success && entityService.TryResolve<IGroundItem>(targetHandle, out var target) && target is not null)
                    {
                        if (!used.ItemScript.UseItemOnGroundItem(used, target, Owner))
                        {
                            if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                            {
                                Owner.SendChatMessage("use_item_on_gitem[used_id=" + used.Id + ",usedWith_id=" + target.ItemOnGround.Id + "]", ChatMessageType.ConsoleText);
                            }
                            else
                            {
                                Owner.SendChatMessage(GameStrings.NothingInterestingHappens);
                            }
                        }
                    }
                    else
                    {
                        Owner.SendChatMessage(GameStrings.YouCantReachThat);
                    }
                });
                Owner.QueueTask(task);
                return true;
            });

            InterfaceInstance.AttachUseOnComponentHandler(0, (componentID, usedWithID, usedWithSlot, usedID, usedSlot) =>
            {
                if (usedWithSlot < 0 || usedWithSlot >= Owner.Inventory.Capacity || usedSlot < 0 || usedSlot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                if (usedSlot == usedWithSlot)
                {
                    return false;
                }

                var usedWith = Owner.Inventory[usedWithSlot];
                if (usedWith == null || usedWith.Id != usedWithID)
                {
                    return false;
                }

                var used = Owner.Inventory[usedSlot];
                if (used == null || used.Id != usedID)
                {
                    return false;
                }

                if (used.ItemScript.UseItemOnItem(used, usedWith, Owner))
                {
                    return true;
                }

                if (usedWith.ItemScript.UseItemOnItem(used, usedWith, Owner))
                {
                    return true;
                }

                if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                {
                    Owner.SendChatMessage("use_item_on_item[used_id=" + used.Id + ",usedWith_id=" + usedWith.Id + "]", ChatMessageType.ConsoleText);
                }
                else
                {
                    Owner.SendChatMessage(GameStrings.NothingInterestingHappens);
                }

                return true;
            });

            InterfaceInstance.AttachUseOnCreatureHandler(0, (componentID, usedOn, forceRun, itemId, slot) =>
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var used = Owner.Inventory[slot];
                if (used == null || used.Id != itemId)
                {
                    return false;
                }

                Owner.ForceRunMovementType(forceRun);
                var task = new CreatureReachTask(Owner, usedOn.Handle, success =>
                {
                    if (success)
                    {
                        switch (usedOn)
                        {
                            case ICharacter character:
                                {
                                    if (used.ItemScript.UseItemOnCharacter(used, character, Owner))
                                    {
                                        return;
                                    }

                                    if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                                    {
                                        Owner.SendChatMessage("use_item_on_character[used_id=" + used.Id + ",usedOn=" + usedOn.Name + "]", ChatMessageType.ConsoleText);
                                    }
                                    else
                                    {
                                        Owner.SendChatMessage(GameStrings.NothingInterestingHappens);
                                    }

                                    break;
                                }
                            case INpc usedOnNpc:
                                {
                                    if (usedOnNpc.Script.UseItemOnNpc(used, Owner))
                                    {
                                        return;
                                    }

                                    if (used.ItemScript.UseItemOnNpc(used, usedOnNpc, Owner))
                                    {
                                        return;
                                    }

                                    if (Owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                                    {
                                        Owner.SendChatMessage("item_used_on_npc[used_id=" + used.Id + ",usedOn=" + usedOn.Name + "]", ChatMessageType.ConsoleText);
                                    }
                                    else
                                    {
                                        Owner.SendChatMessage(GameStrings.NothingInterestingHappens);
                                    }

                                    break;
                                }
                        }
                    }
                    else
                    {
                        Owner.SendChatMessage(GameStrings.YouCantReachThat);
                    }
                });
                Owner.QueueTask(task);
                return true;
            });

            _handler = Owner.RegisterEventHandler(new EventHappened<InventoryChangedEvent>(e =>
            {
                RefreshInventory(e.ChangedSlots);
                return false;
            }));

            RefreshInventory(null);
        }

        /// <summary>
        ///     Refreshes the inventory.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        public void RefreshInventory(HashSet<int>? changedSlots = null) => Owner.Configurations.SendItems(93, false, Owner.Inventory, changedSlots);

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_handler != null)
            {
                Owner.UnregisterEventHandler<InventoryChangedEvent>(_handler);
            }
        }
    }
}
