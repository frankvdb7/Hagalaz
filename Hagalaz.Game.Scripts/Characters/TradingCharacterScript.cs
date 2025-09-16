using System;
using System.Collections.Generic;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Characters
{
    /// <summary>
    ///     Character trading script.
    /// </summary>
    public class TradingCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Contains last requested character.
        /// </summary>
        public ICharacter? LastRequest { get; private set; }

        /// <summary>
        ///     Contains boolean if trade session is currently active.
        /// </summary>
        public bool TradeSession { get; private set; }

        /// <summary>
        ///     Contains trade target.
        /// </summary>
        public ICharacter? Target { get; private set; }

        /// <summary>
        ///     Contains self interface.
        /// </summary>
        public IWidget? SelfInterface { get; private set; }

        /// <summary>
        ///     Contains target interface.
        /// </summary>
        public IWidget? TargetInterface { get; private set; }

        /// <summary>
        ///     Contains self overlay.
        /// </summary>
        public IWidget? SelfOverlay { get; private set; }

        /// <summary>
        ///     Contains target overlay.
        /// </summary>
        public IWidget? TargetOverlay { get; private set; }

        /// <summary>
        ///     Contains boolean if self player accepted.
        /// </summary>
        public bool SelfAccepted { get; private set; }

        /// <summary>
        ///     Contains boolean if target player accepted.
        /// </summary>
        public bool TargetAccepted { get; private set; }

        /// <summary>
        ///     Contains self container instance.
        /// </summary>
        public TradeContainer SelfContainer { get; private set; }

        /// <summary>
        ///     Contains target container instance.
        /// </summary>
        public TradeContainer TargetContainer { get; private set; }

        /// <summary>
        ///     Contains last sended my inventory free slots value.
        /// </summary>
        public int LastMyInventoryFreeSlots { get; private set; }

        /// <summary>
        ///     Contains last target inventory free slots value.
        /// </summary>
        public int LastTargetInventoryFreeSlots { get; private set; }

        /// <summary>
        ///     Contains self int input handler.
        /// </summary>
        public OnIntInput? SelfIntInputHandler { get; private set; }

        /// <summary>
        ///     Contains target int input handler.
        /// </summary>
        public OnIntInput? TargetIntInputHandler { get; private set; }

        /// <summary>
        ///     Contains boolean if self trade was modified.
        /// </summary>
        public bool SelfModified { get; private set; }

        /// <summary>
        ///     Contains boolean if target trade was modified.
        /// </summary>
        public bool TargetModified { get; private set; }

        public TradingCharacterScript(ICharacterContextAccessor contextAccessor, IItemBuilder itemBuilder) : base(contextAccessor)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Happens when character enter's world.
        /// </summary>
        public override void OnRegistered() =>
            Character.RegisterCharactersOptionHandler(CharacterClickType.Option4Click,
                "Trade with",
                65535,
                false,
                (target, forceRun) =>
                {
                    Character.Interrupt(this);
                    Character.ForceRunMovementType(forceRun);
                    Character.QueueTask(new CreatureReachTask(Character,
                        target,
                        success =>
                        {
                            Character.Interrupt(this);
                            if (success)
                            {
                                if (target.IsBusy())
                                {
                                    Character.SendChatMessage("The other player is busy at the moment.");
                                }
                                else
                                {
                                    LastRequest = target;
                                    var targetLastRequest = GetLastRequestOf(target);
                                    if (targetLastRequest == Character || targetLastRequest != null && targetLastRequest.Name.Equals(Character.Name))
                                    {
                                        LastRequest = null;
                                        SetLastRequestOf(target, null);
                                        target.Interrupt(this);
                                        StartTradeSession(target);
                                    }
                                    else
                                    {
                                        Character.SendChatMessage("Sending trade offer...");
                                        target.SendChatMessage("wishes to trade with you.",
                                            ChatMessageType.TradeRequestMessage,
                                            Character.DisplayName,
                                            Character.PreviousDisplayName);
                                    }
                                }
                            }
                            else
                            {
                                Character.SendChatMessage(GameStrings.YouCantReachThat);
                            }
                        }));
                });

        /// <summary>
        ///     Happens when character exits world.
        /// </summary>
        public override void OnDestroy()
        {
            if (TradeSession)
            {
                CancelTradeSession();
            }
        }

        /// <summary>
        ///     Get's called when character is interrupted.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="source">
        ///     Object which performed the interruption,
        ///     this parameter can be null , but it is not encouraged to do so.
        ///     Best use would be to set the invoker class instance as source.
        /// </param>
        public override void OnInterrupt(object source)
        {
            if (source == this)
            {
                return;
            }

            base.OnInterrupt(source);
            CancelTradeSession();
        }

        /// <summary>
        ///     Tick's trading.
        /// </summary>
        public override void Tick()
        {
            if (TradeSession)
            {
                if (Target.IsDestroyed)
                {
                    CancelTradeSession();
                }

                if (Character.IsDestroyed)
                {
                    CancelTradeSession();
                }

                if (!SelfInterface.IsOpened)
                {
                    CancelTradeSession();
                }

                if (!TargetInterface.IsOpened)
                {
                    CancelTradeSession();
                }


                if (SelfInterface.Id == 335 || TargetInterface.Id == 335) // trade offer step
                {
                    if (!SelfOverlay.IsOpened || !TargetOverlay.IsOpened)
                    {
                        CancelTradeSession();
                    }

                    if (Character.Inventory.FreeSlots != LastMyInventoryFreeSlots || Target.Inventory.FreeSlots != LastTargetInventoryFreeSlots)
                    {
                        RefreshFreeInventorySlots();
                    }
                }
                else // trade confirm step
                {
                    if (SelfOverlay.IsOpened || TargetOverlay.IsOpened)
                    {
                        CancelTradeSession();
                    }
                }
            }

            if (LastRequest != null)
            {
                if (LastRequest.IsDestroyed || !Character.Viewport.InBounds(LastRequest.Location))
                {
                    LastRequest = null;
                }
            }
        }

        /// <summary>
        ///     Happens when script instance is initialized.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Start's trade session with specific target.
        /// </summary>
        /// <param name="target"></param>
        public void StartTradeSession(ICharacter target)
        {
            TradeSession = true;
            SelfAccepted = false;
            TargetAccepted = false;
            Target = target;

            Character.Configurations.SendStandardConfiguration(1042, 0);
            Target.Configurations.SendStandardConfiguration(1042, 0);
            Character.Configurations.SendStandardConfiguration(1043, 0);
            Target.Configurations.SendStandardConfiguration(1043, 0);

            var characterTradeInterfaceScript = Character.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            characterTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession)
                {
                    Target.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };
            var targetTradeInterfaceScript = Target.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            targetTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession)
                {
                    Character.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };
            if (!Character.Widgets.OpenWidget(335, 0, characterTradeInterfaceScript, false) ||
                !Target.Widgets.OpenWidget(335, 0, targetTradeInterfaceScript, false))
            {
                TradeSession = false;
                Character.Interrupt(this);
                Target.Interrupt(this);
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                Target = null;
                return;
            }

            SelfInterface = Character.Widgets.GetOpenWidget(335);
            TargetInterface = Target.Widgets.GetOpenWidget(335);
            if (SelfInterface == null || TargetInterface == null)
            {
                TradeSession = false;
                SelfInterface = null;
                TargetInterface = null;
                Character.Interrupt(this);
                Target.Interrupt(this);
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                Target = null;
                return;
            }

            if (!Character.Widgets.OpenInventoryOverlay(336, 1, Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>()) ||
                !Target.Widgets.OpenInventoryOverlay(336, 1, Target.ServiceProvider.GetRequiredService<DefaultWidgetScript>()))
            {
                TradeSession = false;
                SelfInterface = null;
                TargetInterface = null;
                Character.Interrupt(this);
                Target.Interrupt(this);
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                Target = null;
                return;
            }

            SelfOverlay = Character.Widgets.GetOpenWidget(336);
            TargetOverlay = Target.Widgets.GetOpenWidget(336);
            if (SelfOverlay == null || TargetOverlay == null)
            {
                TradeSession = false;
                SelfInterface = null;
                TargetInterface = null;
                SelfOverlay = null;
                TargetOverlay = null;
                Character.Interrupt(this);
                Target.Interrupt(this);
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                Target = null;
                return;
            }

            SelfContainer = [];
            TargetContainer = [];

            SelfInterface.DrawString(17, "Trading With: " + Target.DisplayName);
            TargetInterface.DrawString(17, "Trading With: " + Character.DisplayName);

            // IviiiIsssssssss
            // setupInterfaceItemsDisplayFromItemsArrayNonSplit(icomponent,itemsArrayIndex,numRows,numCollumns,dragOptions,dragTarget,option1,option2,option3,option4,option5,option6,option7,option8,option9) : 150
            // setupInterfaceItemsDisplayFromItemsArraySplit(icomponent,itemsArrayIndex,numRows,numCollumns,dragOptions,dragTarget,option1,option2,option3,option4,option5,option6,option7,option8,option9) : 695
            Character.Configurations.SendCs2Script(150,
            [
                (335 << 16) | 32, 90, 4, 7, 1, -1, "Remove", "Remove-5", "Remove-10", "Remove-All", "Remove-X", "Value"
            ]);
            Character.Configurations.SendCs2Script(695,
            [
                (335 << 16) | 35, 90, 4, 7, 0, -1, "Value", "", "", "", "", "", "", "", ""
            ]);
            Target.Configurations.SendCs2Script(150,
            [
                (335 << 16) | 32, 90, 4, 7, 1, -1, "Remove", "Remove-5", "Remove-10", "Remove-All", "Remove-X", "Value"
            ]);
            Target.Configurations.SendCs2Script(695,
            [
                (335 << 16) | 35, 90, 4, 7, 0, -1, "Value", "", "", "", "", "", "", "", ""
            ]);

            SelfInterface.SetOptions(32,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x400); // allow clicking of 6 right click options + auto examine option ( last )
            SelfInterface.SetOptions(35, 0, 27, 0x2 | 0x400); // allow clicking of one option + auto examine option ( last )
            TargetInterface.SetOptions(32,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x400); // allow clicking of 6 right click options + auto examine option ( last )
            TargetInterface.SetOptions(35, 0, 27, 0x2 | 0x400); // allow clicking of one option + auto examine option ( last )

            Character.Configurations.SendCs2Script(150,
            [
                (336 << 16) | 0, 93, 4, 7, 0, -1, "Offer", "Offer-5", "Offer-10", "Offer-All", "Offer-X", "Value", "Lend"
            ]);
            Target.Configurations.SendCs2Script(150,
            [
                (336 << 16) | 0, 93, 4, 7, 0, -1, "Offer", "Offer-5", "Offer-10", "Offer-All", "Offer-X", "Value", "Lend"
            ]);

            SelfOverlay.SetOptions(0,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )
            TargetOverlay.SetOptions(0,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )

            SelfOverlay.AttachClickHandler(0,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= Character.Inventory.Capacity)
                    {
                        return false;
                    }

                    var item = Character.Inventory[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (!item.ItemScript.CanTradeItem(item, Character))
                    {
                        Character.SendChatMessage("You can't trade this item.");
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick || clickType == ComponentClickType.Option2Click ||
                        clickType == ComponentClickType.Option3Click || clickType == ComponentClickType.Option4Click ||
                        clickType == ComponentClickType.Option5Click)
                    {
                        var count = 1;
                        var max = Character.Inventory.GetCount(item);
                        if (max <= 0)
                        {
                            return false;
                        }

                        if (clickType == ComponentClickType.Option2Click)
                        {
                            count = 5;
                        }
                        else if (clickType == ComponentClickType.Option3Click)
                        {
                            count = 10;
                        }
                        else if (clickType == ComponentClickType.Option4Click)
                        {
                            count = max;
                        }
                        else if (clickType == ComponentClickType.Option5Click)
                        {
                            OnIntInput handler = null;
                            handler = amt =>
                            {
                                Character.Widgets.IntInputHandler = null;
                                if (SelfIntInputHandler != handler)
                                {
                                    return;
                                }

                                SelfIntInputHandler = null;
                                if (amt <= 0)
                                {
                                    return;
                                }

                                var rem = item.Clone();
                                rem.Count = amt > max ? max : amt;
                                var cnt = Character.Inventory.Remove(rem);
                                if (cnt <= 0)
                                {
                                    return;
                                }

                                var add = item.Clone();
                                add.Count = cnt;
                                SelfContainer.Add(add);
                                RefreshTradeOfferScreen();
                                ProcessTradeChange(true, false);
                            };
                            SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                            Character.Configurations.SendIntegerInput("Please enter the amount to offer:");
                            return true;
                        }

                        if (count > 0)
                        {
                            if (count > max)
                            {
                                count = max;
                            }

                            var toRemove = item.Clone();
                            toRemove.Count = count;
                            count = Character.Inventory.Remove(toRemove, itemSlot);
                            if (count <= 0)
                            {
                                return false;
                            }

                            var toAdd = item.Clone();
                            toAdd.Count = count;
                            SelfContainer.Add(toAdd);
                            RefreshTradeOfferScreen();
                            ProcessTradeChange(true, false);
                        }
                    }
                    else if (clickType == ComponentClickType.Option6Click) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Character.SendChatMessage(item.Name + ": market price is " +
                                                      (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Character.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                      item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }
                    else if (clickType == ComponentClickType.Option7Click) // lend
                    {
                        Character.SendChatMessage("Not yet implemented.");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Character.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });

            TargetOverlay.AttachClickHandler(0,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= Target.Inventory.Capacity)
                    {
                        return false;
                    }

                    var item = Target.Inventory[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (!item.ItemScript.CanTradeItem(item, Target))
                    {
                        Target.SendChatMessage("You can't trade this item.");
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick || clickType == ComponentClickType.Option2Click ||
                        clickType == ComponentClickType.Option3Click || clickType == ComponentClickType.Option4Click ||
                        clickType == ComponentClickType.Option5Click)
                    {
                        var count = 1;
                        var max = Target.Inventory.GetCount(item);
                        if (max <= 0)
                        {
                            return false;
                        }

                        if (clickType == ComponentClickType.Option2Click)
                        {
                            count = 5;
                        }
                        else if (clickType == ComponentClickType.Option3Click)
                        {
                            count = 10;
                        }
                        else if (clickType == ComponentClickType.Option4Click)
                        {
                            count = max;
                        }
                        else if (clickType == ComponentClickType.Option5Click)
                        {
                            OnIntInput handler = null;
                            handler = amt =>
                            {
                                Target.Widgets.IntInputHandler = null;
                                if (TargetIntInputHandler != handler)
                                {
                                    return;
                                }

                                TargetIntInputHandler = null;
                                if (amt <= 0)
                                {
                                    return;
                                }

                                var rem = item.Clone();
                                rem.Count = amt > max ? max : amt;
                                var cnt = Target.Inventory.Remove(rem);
                                if (cnt <= 0)
                                {
                                    return;
                                }

                                var add = item.Clone();
                                add.Count = cnt;
                                TargetContainer.Add(add);
                                RefreshTradeOfferScreen();
                                ProcessTradeChange(false, false);
                            };
                            TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                            Target.Configurations.SendIntegerInput("Please enter the amount to offer:");
                            return true;
                        }

                        if (count > 0)
                        {
                            if (count > max)
                            {
                                count = max;
                            }

                            var toRemove = item.Clone();
                            toRemove.Count = count;
                            count = Target.Inventory.Remove(toRemove, itemSlot);
                            if (count <= 0)
                            {
                                return false;
                            }

                            var toAdd = item.Clone();
                            toAdd.Count = count;
                            TargetContainer.Add(toAdd);
                            RefreshTradeOfferScreen();
                            ProcessTradeChange(false, false);
                        }
                    }
                    else if (clickType == ComponentClickType.Option6Click) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Target.SendChatMessage(item.Name + ": market price is " +
                                                   (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Target.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                   item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }
                    else if (clickType == ComponentClickType.Option7Click) // lend
                    {
                        Target.SendChatMessage("Not yet implemented.");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Target.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });

            SelfInterface.AttachClickHandler(32,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= SelfContainer.Capacity)
                    {
                        return false;
                    }

                    var item = SelfContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick || clickType == ComponentClickType.Option2Click ||
                        clickType == ComponentClickType.Option3Click || clickType == ComponentClickType.Option4Click ||
                        clickType == ComponentClickType.Option5Click)
                    {
                        var count = 0;
                        var max = SelfContainer.GetCount(item);
                        if (max <= 0)
                        {
                            return false;
                        }

                        if (clickType == ComponentClickType.LeftClick)
                        {
                            count = 1;
                        }
                        else if (clickType == ComponentClickType.Option2Click)
                        {
                            count = 5;
                        }
                        else if (clickType == ComponentClickType.Option3Click)
                        {
                            count = 10;
                        }
                        else if (clickType == ComponentClickType.Option4Click)
                        {
                            count = max;
                        }
                        else if (clickType == ComponentClickType.Option5Click)
                        {
                            OnIntInput handler = null;
                            handler = amt =>
                            {
                                Character.Widgets.IntInputHandler = null;
                                if (SelfIntInputHandler != handler)
                                {
                                    return;
                                }

                                SelfIntInputHandler = null;
                                if (amt <= 0)
                                {
                                    return;
                                }

                                var rem = item.Clone();
                                rem.Count = amt > max ? max : amt;
                                var cnt = SelfContainer.Remove(rem, itemSlot);
                                if (cnt <= 0)
                                {
                                    return;
                                }

                                var add = item.Clone();
                                add.Count = cnt;
                                Character.Inventory.Add(add);
                                RefreshTradeOfferScreen();
                                ProcessTradeChange(true, true);
                            };
                            SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                            Character.Configurations.SendIntegerInput("Please enter the amount to remove:");
                            return true;
                        }

                        if (count > 0)
                        {
                            if (count > max)
                            {
                                count = max;
                            }

                            var toRemove = item.Clone();
                            toRemove.Count = count;
                            count = SelfContainer.Remove(toRemove, itemSlot);
                            if (count <= 0)
                            {
                                return false;
                            }

                            var toAdd = item.Clone();
                            toAdd.Count = count;
                            Character.Inventory.Add(toAdd);
                            RefreshTradeOfferScreen();
                            ProcessTradeChange(true, true);
                        }
                    }
                    else if (clickType == ComponentClickType.Option6Click) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Character.SendChatMessage(item.Name + ": market price is " +
                                                      (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Character.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                      item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Character.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });
            TargetInterface.AttachClickHandler(32,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= TargetContainer.Capacity)
                    {
                        return false;
                    }

                    var item = TargetContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick || clickType == ComponentClickType.Option2Click ||
                        clickType == ComponentClickType.Option3Click || clickType == ComponentClickType.Option4Click ||
                        clickType == ComponentClickType.Option5Click)
                    {
                        var count = 0;
                        var max = TargetContainer.GetCount(item);
                        if (max <= 0)
                        {
                            return false;
                        }

                        if (clickType == ComponentClickType.LeftClick)
                        {
                            count = 1;
                        }
                        else if (clickType == ComponentClickType.Option2Click)
                        {
                            count = 5;
                        }
                        else if (clickType == ComponentClickType.Option3Click)
                        {
                            count = 10;
                        }
                        else if (clickType == ComponentClickType.Option4Click)
                        {
                            count = max;
                        }
                        else if (clickType == ComponentClickType.Option5Click)
                        {
                            OnIntInput handler = null;
                            handler = amt =>
                            {
                                Target.Widgets.IntInputHandler = null;
                                if (TargetIntInputHandler != handler)
                                {
                                    return;
                                }

                                TargetIntInputHandler = null;
                                if (amt <= 0)
                                {
                                    return;
                                }

                                var rem = item.Clone();
                                rem.Count = amt > max ? max : amt;
                                var cnt = TargetContainer.Remove(rem, itemSlot);
                                if (cnt <= 0)
                                {
                                    return;
                                }

                                var add = item.Clone();
                                add.Count = cnt;
                                Target.Inventory.Add(add);
                                RefreshTradeOfferScreen();
                                ProcessTradeChange(false, true);
                            };
                            TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                            Target.Configurations.SendIntegerInput("Please enter the amount to remove:");
                            return true;
                        }

                        if (count > 0)
                        {
                            if (count > max)
                            {
                                count = max;
                            }

                            var toRemove = item.Clone();
                            toRemove.Count = count;
                            count = TargetContainer.Remove(toRemove, itemSlot);
                            if (count <= 0)
                            {
                                return false;
                            }

                            var toAdd = item.Clone();
                            toAdd.Count = count;
                            Target.Inventory.Add(toAdd);
                            RefreshTradeOfferScreen();
                            ProcessTradeChange(false, true);
                        }
                    }
                    else if (clickType == ComponentClickType.Option6Click) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Target.SendChatMessage(item.Name + ": market price is " +
                                                   (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Target.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                   item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Target.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });

            SelfInterface.AttachClickHandler(35,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= TargetContainer.Capacity)
                    {
                        return false;
                    }

                    var item = TargetContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Character.SendChatMessage(item.Name + ": market price is " +
                                                      (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Character.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                      item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }

                    if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Character.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });

            TargetInterface.AttachClickHandler(35,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= SelfContainer.Capacity)
                    {
                        return false;
                    }

                    var item = SelfContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick) // value
                    {
                        var count = item.Count;
                        if (count == 1)
                        {
                            Target.SendChatMessage(item.Name + ": market price is " +
                                                   (item.ItemDefinition.TradeValue == 1 ? "one coin." : item.ItemDefinition.TradeValue + " coins."));
                        }
                        else
                        {
                            Target.SendChatMessage(item.Name + ": market price is " + item.ItemDefinition.TradeValue + " coins each (" +
                                                   item.ItemDefinition.TradeValue * (long)count + " coins for " + count + ")");
                        }

                        return true;
                    }

                    if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Target.SendChatMessage(item.ItemScript.GetExamine(item));
                        return true;
                    }

                    return true;
                });

            SelfInterface.AttachClickHandler(53,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    OnIntInput handler = null;
                    handler = amt =>
                    {
                        Character.Widgets.IntInputHandler = null;
                        if (SelfIntInputHandler != handler)
                        {
                            return;
                        }

                        SelfIntInputHandler = null;
                        if (amt <= 0)
                        {
                            return;
                        }

                        amt = Character.MoneyPouch.Remove(amt);
                        if (amt <= 0)
                        {
                            return;
                        }

                        SelfContainer.Add(_itemBuilder.Create().WithId(995).WithCount(amt).Build());
                        RefreshTradeOfferScreen();
                        ProcessTradeChange(true, false);
                    };
                    SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                    Character.Configurations.SendIntegerInput(Character.MoneyPouch.Examine + "<br>How many would you like to offer?");
                    return true;
                });

            TargetInterface.AttachClickHandler(53,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    OnIntInput handler = null;
                    handler = amt =>
                    {
                        Target.Widgets.IntInputHandler = null;
                        if (TargetIntInputHandler != handler)
                        {
                            return;
                        }

                        TargetIntInputHandler = null;
                        if (amt <= 0)
                        {
                            return;
                        }

                        amt = Target.MoneyPouch.Remove(amt);
                        if (amt <= 0)
                        {
                            return;
                        }

                        TargetContainer.Add(_itemBuilder.Create().WithId(995).WithCount(amt).Build());
                        RefreshTradeOfferScreen();
                        ProcessTradeChange(false, false);
                    };
                    TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                    Target.Configurations.SendIntegerInput(Target.MoneyPouch.Examine + "<br>How many would you like to offer?");
                    return true;
                });

            SelfInterface.AttachClickHandler(18,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (SelfAccepted)
                    {
                        return true;
                    }

                    SelfAccepted = true;
                    RefreshTradeConfirmationStatus();
                    return true;
                });
            TargetInterface.AttachClickHandler(18,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (TargetAccepted)
                    {
                        return true;
                    }

                    TargetAccepted = true;
                    RefreshTradeConfirmationStatus();
                    return true;
                });
            SelfInterface.AttachClickHandler(20,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Target.SendChatMessage("The other player declined trade.");
                    CancelTradeSession();
                    return true;
                });
            TargetInterface.AttachClickHandler(20,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Character.SendChatMessage("The other player declined trade.");
                    CancelTradeSession();
                    return true;
                });

            RefreshTradeConfirmationStatus();
            RefreshFreeInventorySlots();
            RefreshTradeOfferScreen();
        }


        /// <summary>
        ///     Process'es trade change.
        /// </summary>
        /// <param name="self">if set to <c>true</c> [self].</param>
        /// <param name="valueDecreased">if set to <c>true</c> [value decreased].</param>
        public void ProcessTradeChange(bool self, bool valueDecreased)
        {
            if (!TradeSession)
            {
                return;
            }

            var selfAccepted = SelfAccepted;
            var targetAccepted = TargetAccepted;
            var accepted = selfAccepted | targetAccepted;
            SelfAccepted = false;
            TargetAccepted = false;
            if (accepted)
            {
                RefreshTradeConfirmationStatus();
            }

            var slots = self ? SelfContainer.Updates : TargetContainer.Updates;

            if (valueDecreased)
            {
                if (self && targetAccepted)
                {
                    TargetInterface.DrawString(39, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");
                }
                else if (!self && selfAccepted)
                {
                    SelfInterface.DrawString(39, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");
                }

                foreach (short slot in slots)
                {
                    Character.Configurations.SendCs2Script(143,
                    [
                        (335 << 16) | (self ? 32 : 35), 4, 7, (int)slot
                    ]);
                    Target.Configurations.SendCs2Script(143,
                    [
                        (335 << 16) | (self ? 35 : 32), 4, 7, (int)slot
                    ]);
                }

                if (!SelfModified && self)
                {
                    Character.Configurations.SendStandardConfiguration(1042, 1);
                    Target.Configurations.SendStandardConfiguration(1043, 1);
                }
                else if (!TargetModified && !self)
                {
                    Character.Configurations.SendStandardConfiguration(1043, 1);
                    Target.Configurations.SendStandardConfiguration(1042, 1);
                }

                if (self)
                {
                    SelfModified = true;
                }
                else
                {
                    TargetModified = true;
                }
            }

            slots.Clear();
        }


        /// <summary>
        ///     Refreshe's free inventory slots.
        /// </summary>
        public void RefreshFreeInventorySlots()
        {
            if (!TradeSession)
            {
                return;
            }

            LastMyInventoryFreeSlots = Character.Inventory.FreeSlots;
            LastTargetInventoryFreeSlots = Target.Inventory.FreeSlots;
            Character.Configurations.SendGlobalCs2String(203,
                "<br><br>" + Target.DisplayName + "<br>has " + LastTargetInventoryFreeSlots + " free<br>inventory slots.");
            Target.Configurations.SendGlobalCs2String(203,
                "<br><br>" + Character.DisplayName + "<br>has " + LastMyInventoryFreeSlots + " free<br>inventory slots.");
        }


        /// <summary>
        ///     Refreshe's trade offer screen ( Items and wealth )
        /// </summary>
        public void RefreshTradeOfferScreen()
        {
            if (!TradeSession)
            {
                return;
            }

            Character.Configurations.SendItems(90, false, SelfContainer, SelfContainer.Updates);
            Character.Configurations.SendItems(90, true, TargetContainer, TargetContainer.Updates);
            Target.Configurations.SendItems(90, false, TargetContainer, TargetContainer.Updates);
            Target.Configurations.SendItems(90, true, SelfContainer, SelfContainer.Updates);

            var selfTotal = SelfContainer.CalculateTotalValue();
            var targetTotal = TargetContainer.CalculateTotalValue();

            Character.Configurations.SendGlobalCs2Int(729, selfTotal);
            Character.Configurations.SendGlobalCs2Int(697, targetTotal);

            Target.Configurations.SendGlobalCs2Int(729, targetTotal);
            Target.Configurations.SendGlobalCs2Int(697, selfTotal);
        }

        /// <summary>
        ///     Refreshe's trade confirmation status.
        /// </summary>
        public void RefreshTradeConfirmationStatus()
        {
            if (!TradeSession)
            {
                return;
            }

            if (SelfInterface.Id == 335 || TargetInterface.Id == 335)
            {
                if (!SelfAccepted && !TargetAccepted)
                {
                    SelfInterface.DrawString(39, ""); // turn off Waiting for other player
                    TargetInterface.DrawString(39, ""); // turn off Waiting for other player
                }
                else if (SelfAccepted && !TargetAccepted)
                {
                    SelfInterface.DrawString(39, "Waiting for other player...");
                    TargetInterface.DrawString(39, "The other player has accepted.");
                }
                else if (!SelfAccepted && TargetAccepted)
                {
                    SelfInterface.DrawString(39, "The other player has accepted.");
                    TargetInterface.DrawString(39, "Waiting for other player...");
                }
                else // GOTO next step
                {
                    StartConfirmationStage();
                }
            }
            else
            {
                if (!SelfAccepted && !TargetAccepted)
                {
                    SelfInterface.DrawString(34, "Are you sure you want to make this trade?");
                    TargetInterface.DrawString(34, "Are you sure you want to make this trade?");
                }
                else if (SelfAccepted && !TargetAccepted)
                {
                    SelfInterface.DrawString(34, "Waiting for other player...");
                    TargetInterface.DrawString(34, "The other player has accepted.");
                }
                else if (!SelfAccepted && TargetAccepted)
                {
                    SelfInterface.DrawString(34, "The other player has accepted.");
                    TargetInterface.DrawString(34, "Waiting for other player...");
                }
                else
                {
                    FinishTradeSession();
                }
            }
        }

        /// <summary>
        ///     Start's trade confirmation stage.
        /// </summary>
        public void StartConfirmationStage()
        {
            if (!TradeSession)
            {
                return;
            }

            SelfAccepted = false;
            TargetAccepted = false;
            ((TradeInterfaceScript)SelfInterface.Script).CloseHandler = null!;
            ((TradeInterfaceScript)TargetInterface.Script).CloseHandler = null!;
            Character.Widgets.CloseWidget(SelfInterface);
            Target.Widgets.CloseWidget(TargetInterface);
            Character.Widgets.CloseWidget(SelfOverlay);
            Target.Widgets.CloseWidget(TargetOverlay);
            SelfIntInputHandler = null;
            TargetIntInputHandler = null;

            var characterTradeInterfaceScript = Character.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            characterTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession)
                {
                    Target.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };

            if (!Character.Widgets.OpenWidget(334,
                    0,
                    characterTradeInterfaceScript,
                    false))
            {
                CancelTradeSession();
            }

            var targetTradeInterfaceScript = Target.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            targetTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession)
                {
                    Character.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };
            if (!Target.Widgets.OpenWidget(334,
                    0,
                    targetTradeInterfaceScript,
                    false))
            {
                CancelTradeSession();
            }

            var self = Character.Widgets.GetOpenWidget(334);
            var target = Target.Widgets.GetOpenWidget(334);
            if (self == null || target == null)
            {
                CancelTradeSession();
                return;
            }

            SelfInterface = self;
            TargetInterface = target;

            Character.Configurations.SendGlobalCs2String(203, Target.DisplayName);
            Target.Configurations.SendGlobalCs2String(203, Character.DisplayName);

            if (SelfModified)
            {
                TargetInterface.SetVisible(55, true);
            }

            if (TargetModified)
            {
                SelfInterface.SetVisible(55, true);
            }

            RefreshTradeConfirmationStatus();

            SelfInterface.AttachClickHandler(21,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (SelfAccepted)
                    {
                        return true;
                    }

                    SelfAccepted = true;
                    RefreshTradeConfirmationStatus();
                    return true;
                });

            TargetInterface.AttachClickHandler(21,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (TargetAccepted)
                    {
                        return true;
                    }

                    TargetAccepted = true;
                    RefreshTradeConfirmationStatus();
                    return true;
                });

            SelfInterface.AttachClickHandler(22,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Target.SendChatMessage("The other player declined trade.");
                    CancelTradeSession();
                    return true;
                });
            TargetInterface.AttachClickHandler(22,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Character.SendChatMessage("The other player declined trade.");
                    CancelTradeSession();
                    return true;
                });
        }

        /// <summary>
        ///     End's trade session.
        /// </summary>
        public void CancelTradeSession()
        {
            if (TradeSession)
            {
                TradeSession = false;
                if (Character.Widgets.IntInputHandler == SelfIntInputHandler)
                {
                    Character.Widgets.IntInputHandler = null;
                }

                if (Target.Widgets.IntInputHandler == TargetIntInputHandler)
                {
                    Target.Widgets.IntInputHandler = null;
                }

                if (SelfInterface.IsOpened)
                {
                    Character.Widgets.CloseWidget(SelfInterface);
                }

                if (TargetInterface.IsOpened)
                {
                    Target.Widgets.CloseWidget(TargetInterface);
                }

                if (SelfOverlay.IsOpened)
                {
                    Character.Widgets.CloseWidget(SelfOverlay);
                }

                if (TargetOverlay.IsOpened)
                {
                    Target.Widgets.CloseWidget(TargetOverlay);
                }

                if (SelfContainer.TakenSlots > 0)
                {
                    for (var i = 0; i < SelfContainer.Capacity; i++)
                    {
                        if (SelfContainer[(short)i] != null)
                        {
                            if (SelfContainer[(short)i].Id == 995)
                            {
                                Character.MoneyPouch.Add(SelfContainer[(short)i].Count);
                            }
                            else
                            {
                                Character.Inventory.Add(SelfContainer[(short)i]);
                            }
                        }
                    }
                }

                if (TargetContainer.TakenSlots > 0)
                {
                    for (var i = 0; i < TargetContainer.Capacity; i++)
                    {
                        if (TargetContainer[(short)i] != null)
                        {
                            if (TargetContainer[(short)i].Id == 995)
                            {
                                Target.MoneyPouch.Add(TargetContainer[(short)i].Count);
                            }
                            else
                            {
                                Target.Inventory.Add(TargetContainer[(short)i]);
                            }
                        }
                    }
                }

                Target = null;
                SelfInterface = null;
                TargetInterface = null;
                SelfOverlay = null;
                TargetOverlay = null;
                SelfAccepted = false;
                TargetAccepted = false;
                SelfContainer = null;
                TargetContainer = null;
                SelfIntInputHandler = null;
                TargetIntInputHandler = null;
            }
        }

        /// <summary>
        ///     Finishe's trade session by exchanging items and closing interfaces.
        /// </summary>
        public void FinishTradeSession()
        {
            if (!TradeSession)
            {
                return;
            }

            var myItems = new IItem[SelfContainer.TakenSlots];
            var targetItems = new IItem[TargetContainer.TakenSlots];
            for (int write = 0, i = 0; i < SelfContainer.Capacity; i++)
            {
                if (SelfContainer[i] != null)
                {
                    myItems[write++] = SelfContainer[i]!;
                }
            }

            for (int write = 0, i = 0; i < TargetContainer.Capacity; i++)
            {
                if (TargetContainer[i] != null)
                {
                    targetItems[write++] = TargetContainer[i]!;
                }
            }

            if (!Character.Inventory.HasSpaceForRange(targetItems))
            {
                Character.SendChatMessage("You don't have enough space in your inventory for this trade.");
                Target!.SendChatMessage("Other player doesn't have enough space in their inventory for this trade.");
                CancelTradeSession();
                return;
            }

            if (!Target!.Inventory.HasSpaceForRange(myItems))
            {
                Character.SendChatMessage("Other player doesn't have enough space in their inventory for this trade.");
                Target.SendChatMessage("You don't have enough space in your inventory for this trade.");
                CancelTradeSession();
                return;
            }

            foreach (var t in targetItems)
            {
                if (t.Id == 995)
                {
                    Character.MoneyPouch.Add(t.Count);
                }
                else
                {
                    Character.Inventory.Add(t);
                }
            }

            foreach (var t in myItems)
            {
                if (t.Id == 995)
                {
                    Target.MoneyPouch.Add(t.Count);
                }
                else
                {
                    Target.Inventory.Add(t);
                }
            }

            SelfContainer.Clear(false);
            TargetContainer.Clear(false);

            Character.SendChatMessage("Accepted trade.");
            Target.SendChatMessage("Accepted trade.");
            CancelTradeSession(); // end
        }

        /// <summary>
        ///     Get's last request of the other character.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static ICharacter? GetLastRequestOf(ICharacter other) => other.GetScript<TradingCharacterScript>()?.LastRequest;

        /// <summary>
        ///     Set's last request of the other character.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="request"></param>
        public static void SetLastRequestOf(ICharacter other, ICharacter? request)
        {
            if (other.TryGetScript<TradingCharacterScript>(out var script))
            {
                script.LastRequest = request;
            }
        }

        /// <summary>
        ///     Contains trade interface script.
        /// </summary>
        public class TradeInterfaceScript : WidgetScript
        {
            /// <summary>
            ///     Contains close handler for this trade interface.
            /// </summary>
            public Action CloseHandler { get; set; }

            public TradeInterfaceScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

            /// <summary>
            ///     Happens when this interface is opened.
            /// </summary>
            public override void OnOpen() { }

            /// <summary>
            ///     Happens when this interface is closed.
            /// </summary>
            public override void OnClose() => CloseHandler.Invoke();
        }

        /// <summary>
        ///     Container for holding items in trade offer interfaces.
        /// </summary>
        public class TradeContainer : BaseItemContainer
        {
            /// <summary>
            ///     Contains last slots update.
            /// </summary>
            public HashSet<int> Updates { get; }

            /// <summary>
            ///     Construct's new trade container.
            /// </summary>
            public TradeContainer()
                : base(StorageType.Normal, 14)
            {
                Updates = [];
                OnUpdate();
            }

            /// <summary>
            ///     Happens when trade container get's updated.
            /// </summary>
            /// <param name="slots"></param>
            public override void OnUpdate(HashSet<int>? slots = null)
            {
                if (slots == null)
                {
                    Updates.Clear();
                    for (var i = 0; i < Capacity; i++)
                    {
                        Updates.Add(i);
                    }
                }
                else
                {
                    Updates.AddRange(slots);
                }
            }

            /// <summary>
            ///     Calculate's total value of this container.
            /// </summary>
            /// <returns></returns>
            public int CalculateTotalValue()
            {
                var total = 0;
                for (var slot = 0; slot < Capacity; slot++)
                {
                    var item = this[slot];
                    if (item == null)
                    {
                        continue;
                    }

                    if ((ulong)total + (ulong)item.ItemDefinition.TradeValue * (ulong)item.Count > int.MaxValue)
                    {
                        return -1;
                    }

                    total += item.ItemDefinition.TradeValue * item.Count;
                }

                return total;
            }
        }
    }
}