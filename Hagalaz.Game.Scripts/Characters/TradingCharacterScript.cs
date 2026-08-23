using System;
using System.Collections.Generic;
using System.Linq;
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
        private TradeSessionState? _tradeSession;
        private TradeSessionState? _linkedTradeSession;

        private enum TradeState
        {
            Active,
            Completing,
            Completed,
            Cancelled
        }

        private sealed class TradeSessionState
        {
            public object Gate { get; } = new();
            public TradingCharacterScript Owner { get; }
            public ICharacter Target { get; }
            public TradingCharacterScript? TargetScript { get; set; }
            public TradeState State { get; set; } = TradeState.Active;

            public TradeSessionState(TradingCharacterScript owner, ICharacter target)
            {
                Owner = owner;
                Target = target;
            }
        }

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

        private int? SelfAcceptedContainerRevision { get; set; }
        private int? TargetAcceptedContainerRevision { get; set; }

        public TradingCharacterScript(ICharacterContextAccessor contextAccessor, IItemBuilder itemBuilder) : base(contextAccessor)
        {
            SelfContainer = new TradeContainer();
            TargetContainer = new TradeContainer();
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
            CancelTradeSession(forceConservation: true);
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
            var session = _tradeSession;
            if (session != null)
            {
                lock (session.Gate)
                {
                    if (IsActiveSession(session) && HasTradeWidgets())
                    {
                        if (ShouldCancelTrade())
                        {
                            CancelTradeSession(session, forceConservation: false);
                        }
                        else if (IsOfferStage() && HasInventorySlotChange())
                        {
                            RefreshFreeInventorySlots();
                        }
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

        private bool IsOfferStage() => SelfInterface?.Id == 335 || TargetInterface?.Id == 335;

        private bool HasTradeWidgets()
        {
            if (Target == null)
            {
                return false;
            }

            if (SelfInterface == null)
            {
                return false;
            }

            if (TargetInterface == null)
            {
                return false;
            }

            return SelfOverlay != null && TargetOverlay != null;
        }

        private bool HasInventorySlotChange()
        {
            var target = Target;
            if (target == null)
            {
                return false;
            }

            if (Character.Inventory.FreeSlots != LastMyInventoryFreeSlots)
            {
                return true;
            }

            return target.Inventory.FreeSlots != LastTargetInventoryFreeSlots;
        }

        private bool ShouldCancelTrade()
        {
            var target = Target;
            var selfInterface = SelfInterface;
            var targetInterface = TargetInterface;
            var selfOverlay = SelfOverlay;
            var targetOverlay = TargetOverlay;
            if (target == null)
            {
                return false;
            }

            if (selfInterface == null || targetInterface == null)
            {
                return false;
            }

            if (selfOverlay == null || targetOverlay == null)
            {
                return false;
            }

            if (target.IsDestroyed || Character.IsDestroyed)
            {
                return true;
            }

            if (!selfInterface.IsOpened || !targetInterface.IsOpened)
            {
                return true;
            }

            return IsOfferStage()
                ? !selfOverlay.IsOpened || !targetOverlay.IsOpened
                : selfOverlay.IsOpened || targetOverlay.IsOpened;
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
            if (_tradeSession != null)
            {
                return;
            }

            var session = new TradeSessionState(this, target);
            _tradeSession = session;
            SelfContainer = new TradeContainer();
            TargetContainer = new TradeContainer();
            TradeSession = true;
            SelfAccepted = false;
            TargetAccepted = false;
            SelfAcceptedContainerRevision = null;
            TargetAcceptedContainerRevision = null;
            SelfModified = false;
            TargetModified = false;
            Target = target;

            session.TargetScript = target.GetScript<TradingCharacterScript>();
            session.TargetScript?.LinkTradeSession(session);

            Character.Configurations.SendStandardConfiguration(1042, 0);
            Target.Configurations.SendStandardConfiguration(1042, 0);
            Character.Configurations.SendStandardConfiguration(1043, 0);
            Target.Configurations.SendStandardConfiguration(1043, 0);

            var characterTradeInterfaceScript = Character.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            characterTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession && Target != null && SelfInterface != null && TargetInterface != null && SelfOverlay != null && TargetOverlay != null)
                {
                    Target.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };
            var targetTradeInterfaceScript = Target.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            targetTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession && Target != null && SelfInterface != null && TargetInterface != null && SelfOverlay != null && TargetOverlay != null)
                {
                    Character.SendChatMessage("The other player declined trade.");
                }

                CancelTradeSession();
            };
            if (!Character.Widgets.OpenWidget(335, 0, characterTradeInterfaceScript, false) ||
                !Target.Widgets.OpenWidget(335, 0, targetTradeInterfaceScript, false))
            {
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                CancelTradeSession();
                return;
            }

            SelfInterface = Character.Widgets.GetOpenWidget(335);
            TargetInterface = Target.Widgets.GetOpenWidget(335);
            if (SelfInterface == null || TargetInterface == null)
            {
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                CancelTradeSession();
                return;
            }

            if (!Character.Widgets.OpenInventoryOverlay(336, 1, Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>()) ||
                !Target.Widgets.OpenInventoryOverlay(336, 1, Target.ServiceProvider.GetRequiredService<DefaultWidgetScript>()))
            {
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                CancelTradeSession();
                return;
            }

            SelfOverlay = Character.Widgets.GetOpenWidget(336);
            TargetOverlay = Target.Widgets.GetOpenWidget(336);
            if (SelfOverlay == null || TargetOverlay == null)
            {
                Character.SendChatMessage("System error occured.");
                Target.SendChatMessage("System error occured.");
                CancelTradeSession();
                return;
            }

            SelfInterface?.DrawString(17, "Trading With: " + Target.DisplayName);
            TargetInterface?.DrawString(17, "Trading With: " + Character.DisplayName);

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

                                TryOfferInventoryItem(true, item, amt > max ? max : amt, -1);
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

                            if (!TryOfferInventoryItem(true, item, count, itemSlot))
                            {
                                return false;
                            }
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

                                TryOfferInventoryItem(false, item, amt > max ? max : amt, -1);
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

                            if (!TryOfferInventoryItem(false, item, count, itemSlot))
                            {
                                return false;
                            }
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

                                TryRemoveOfferedItem(true, item, amt > max ? max : amt, itemSlot);
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

                            if (!TryRemoveOfferedItem(true, item, count, itemSlot))
                            {
                                return false;
                            }
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

                                TryRemoveOfferedItem(false, item, amt > max ? max : amt, itemSlot);
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

                            if (!TryRemoveOfferedItem(false, item, count, itemSlot))
                            {
                                return false;
                            }
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

                        TryOfferMoney(true, amt);
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

                        TryOfferMoney(false, amt);
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

                    AcceptTrade(true);
                    return true;
                });
            TargetInterface.AttachClickHandler(18,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(false);
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


        private bool TryOfferInventoryItem(bool self, IItem item, int requestedCount, int preferredSlot)
        {
            var session = _tradeSession;
            if (session == null)
            {
                return false;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session))
                {
                    return false;
                }

                var character = self ? Character : session.Target;
                var offer = self ? SelfContainer : TargetContainer;
                var count = Math.Min(requestedCount, character.Inventory.GetCount(item));
                if (count <= 0)
                {
                    return false;
                }

                var toRemove = item.Clone();
                toRemove.Count = count;
                if (!offer.HasSpaceFor(toRemove))
                {
                    return false;
                }

                var removed = character.Inventory.Remove(toRemove, preferredSlot);
                if (removed <= 0)
                {
                    return false;
                }

                var toAdd = item.Clone();
                toAdd.Count = removed;
                if (TradeExchange.AddRangeForTrade(offer, [toAdd]))
                {
                    RefreshTradeOfferScreenLocked(session);
                    ProcessTradeChangeLocked(session, self, false);
                    return true;
                }

                character.Inventory.Add(toAdd);
                return false;
            }
        }

        private bool TryRemoveOfferedItem(bool self, IItem item, int requestedCount, int preferredSlot)
        {
            var session = _tradeSession;
            if (session == null)
            {
                return false;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session))
                {
                    return false;
                }

                var character = self ? Character : session.Target;
                var offer = self ? SelfContainer : TargetContainer;
                var count = Math.Min(requestedCount, offer.GetCount(item));
                if (count <= 0)
                {
                    return false;
                }

                var toRemove = item.Clone();
                toRemove.Count = count;
                var toAdd = item.Clone();
                toAdd.Count = count;
                if (item.Id != 995 && !character.Inventory.HasSpaceFor(toAdd))
                {
                    return false;
                }

                if (!TradeExchange.RemoveForTrade(offer, toRemove, preferredSlot))
                {
                    return false;
                }

                toAdd.Count = count;
                if (item.Id == 995)
                {
                    if (!TradeExchange.AddMoney(character, count))
                    {
                        offer.Add(toAdd);
                        return false;
                    }
                }
                else
                {
                    if (!TradeExchange.AddRangeForTrade(character.Inventory, [toAdd]))
                    {
                        offer.Add(toAdd);
                        return false;
                    }
                }

                RefreshTradeOfferScreenLocked(session);
                ProcessTradeChangeLocked(session, self, true);
                return true;
            }
        }

        private bool TryOfferMoney(bool self, int requestedCount)
        {
            var session = _tradeSession;
            if (session == null)
            {
                return false;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session) || requestedCount <= 0)
                {
                    return false;
                }

                var character = self ? Character : session.Target;
                var offer = self ? SelfContainer : TargetContainer;
                var coinOffer = _itemBuilder.Create().WithId(995).WithCount(requestedCount).Build();
                if (!offer.HasSpaceFor(coinOffer))
                {
                    return false;
                }

                if (!character.MoneyPouch.Contains(995, requestedCount) ||
                    !TradeExchange.AddRangeForTrade(offer, [coinOffer]))
                {
                    return false;
                }

                if (TradeExchange.RemoveMoney(character, requestedCount))
                {
                    RefreshTradeOfferScreenLocked(session);
                    ProcessTradeChangeLocked(session, self, false);
                    return true;
                }

                TradeExchange.RemoveForTrade(offer, coinOffer);
                return false;
            }
        }

        private bool IsActiveSession(TradeSessionState session) =>
            ReferenceEquals(_tradeSession, session) && TradeSession && session.State == TradeState.Active;

        /// <summary>
        ///     Process'es trade change.
        /// </summary>
        /// <param name="self">if set to <c>true</c> [self].</param>
        /// <param name="valueDecreased">if set to <c>true</c> [value decreased].</param>
        public void ProcessTradeChange(bool self, bool valueDecreased)
        {
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                ProcessTradeChangeLocked(session, self, valueDecreased);
            }
        }

        private void ProcessTradeChangeLocked(TradeSessionState session, bool self, bool valueDecreased)
        {
            if (!IsActiveSession(session))
            {
                return;
            }

            var selfAccepted = SelfAccepted;
            var targetAccepted = TargetAccepted;
            var accepted = selfAccepted | targetAccepted;
            SelfAccepted = false;
            TargetAccepted = false;
            SelfAcceptedContainerRevision = null;
            TargetAcceptedContainerRevision = null;
            if (accepted)
            {
                RefreshTradeConfirmationStatusLocked(session);
            }

            var slots = self ? SelfContainer.Updates : TargetContainer.Updates;

            if (valueDecreased)
            {
                if (self && targetAccepted)
                {
                    TargetInterface?.DrawString(39, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");
                }
                else if (!self && selfAccepted)
                {
                    SelfInterface?.DrawString(39, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");
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
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session))
                {
                    return;
                }

                LastMyInventoryFreeSlots = Character.Inventory.FreeSlots;
                LastTargetInventoryFreeSlots = session.Target.Inventory.FreeSlots;
                Character.Configurations.SendGlobalCs2String(203,
                    "<br><br>" + session.Target.DisplayName + "<br>has " + LastTargetInventoryFreeSlots + " free<br>inventory slots.");
                session.Target.Configurations.SendGlobalCs2String(203,
                    "<br><br>" + Character.DisplayName + "<br>has " + LastMyInventoryFreeSlots + " free<br>inventory slots.");
            }
        }


        private void AcceptTrade(bool self)
        {
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session))
                {
                    return;
                }

                if (self)
                {
                    if (SelfAccepted)
                    {
                        return;
                    }

                    SelfAccepted = true;
                    SelfAcceptedContainerRevision = SelfContainer.Revision;
                }
                else
                {
                    if (TargetAccepted)
                    {
                        return;
                    }

                    TargetAccepted = true;
                    TargetAcceptedContainerRevision = TargetContainer.Revision;
                }

                RefreshTradeConfirmationStatusLocked(session);
            }
        }

        /// <summary>
        ///     Refreshe's trade offer screen ( Items and wealth )
        /// </summary>
        public void RefreshTradeOfferScreen()
        {
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                RefreshTradeOfferScreenLocked(session);
            }
        }

        private void RefreshTradeOfferScreenLocked(TradeSessionState session)
        {
            if (!IsActiveSession(session))
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
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                RefreshTradeConfirmationStatusLocked(session);
            }
        }

        private void RefreshTradeConfirmationStatusLocked(TradeSessionState session)
        {
            if (!IsActiveSession(session))
            {
                return;
            }

            if (SelfInterface?.Id == 335 || TargetInterface?.Id == 335)
            {
                if (!SelfAccepted && !TargetAccepted)
                {
                    SelfInterface?.DrawString(39, ""); // turn off Waiting for other player
                    TargetInterface?.DrawString(39, ""); // turn off Waiting for other player
                }
                else if (SelfAccepted && !TargetAccepted)
                {
                    SelfInterface?.DrawString(39, "Waiting for other player...");
                    TargetInterface?.DrawString(39, "The other player has accepted.");
                }
                else if (!SelfAccepted && TargetAccepted)
                {
                    SelfInterface?.DrawString(39, "The other player has accepted.");
                    TargetInterface?.DrawString(39, "Waiting for other player...");
                }
                else // GOTO next step
                {
                    StartConfirmationStageLocked(session);
                }
            }
            else
            {
                if (!SelfAccepted && !TargetAccepted)
                {
                    SelfInterface?.DrawString(34, "Are you sure you want to make this trade?");
                    TargetInterface?.DrawString(34, "Are you sure you want to make this trade?");
                }
                else if (SelfAccepted && !TargetAccepted)
                {
                    SelfInterface?.DrawString(34, "Waiting for other player...");
                    TargetInterface?.DrawString(34, "The other player has accepted.");
                }
                else if (!SelfAccepted && TargetAccepted)
                {
                    SelfInterface?.DrawString(34, "The other player has accepted.");
                    TargetInterface?.DrawString(34, "Waiting for other player...");
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
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                StartConfirmationStageLocked(session);
            }
        }

        private void StartConfirmationStageLocked(TradeSessionState session)
        {
            if (!IsActiveSession(session))
            {
                return;
            }

            SelfAccepted = false;
            TargetAccepted = false;
            SelfAcceptedContainerRevision = null;
            TargetAcceptedContainerRevision = null;
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
                if (TradeSession && Target != null && SelfInterface != null && TargetInterface != null && SelfOverlay != null && TargetOverlay != null)
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
                return;
            }

            var targetTradeInterfaceScript = Target.ServiceProvider.GetRequiredService<TradeInterfaceScript>();
            targetTradeInterfaceScript.CloseHandler = () =>
            {
                if (TradeSession && Target != null && SelfInterface != null && TargetInterface != null && SelfOverlay != null && TargetOverlay != null)
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
                return;
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

            RefreshTradeConfirmationStatusLocked(session);

            SelfInterface.AttachClickHandler(21,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(true);
                    return true;
                });

            TargetInterface.AttachClickHandler(21,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(false);
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
            CancelTradeSession(forceConservation: false);
        }

        private void CancelTradeSession(bool forceConservation)
        {
            var session = _tradeSession ?? _linkedTradeSession;
            if (session == null)
            {
                return;
            }

            if (!ReferenceEquals(session.Owner, this))
            {
                session.Owner.CancelTradeSession(session, forceConservation);
                return;
            }

            CancelTradeSession(session, forceConservation);
        }

        private void LinkTradeSession(TradeSessionState session) => _linkedTradeSession = session;

        private void CancelTradeSession(TradeSessionState session, bool forceConservation)
        {
            lock (session.Gate)
            {
                if (session.State is TradeState.Completed or TradeState.Cancelled)
                {
                    return;
                }

                if (session.State == TradeState.Completing)
                {
                    return;
                }

                if (!TradeExchange.TryRefundTrade(Character, SelfContainer, session.Target, TargetContainer, _itemBuilder))
                {
                    if (forceConservation &&
                        TradeExchange.TryConserveEscrow(
                            Character,
                            SelfContainer,
                            session.Target,
                            TargetContainer))
                    {
                        session.State = TradeState.Cancelled;
                        ResetTradeSessionLocked(session);
                    }

                    return;
                }

                session.State = TradeState.Cancelled;
                ResetTradeSessionLocked(session);
            }
        }

        /// <summary>
        ///     Finishe's trade session by exchanging items and closing interfaces.
        /// </summary>
        public void FinishTradeSession()
        {
            var session = _tradeSession;
            if (session == null)
            {
                return;
            }

            lock (session.Gate)
            {
                if (!IsActiveSession(session) || !SelfAccepted || !TargetAccepted)
                {
                    return;
                }

                if (SelfAcceptedContainerRevision != SelfContainer.Revision ||
                    TargetAcceptedContainerRevision != TargetContainer.Revision)
                {
                    SelfAccepted = false;
                    TargetAccepted = false;
                    SelfAcceptedContainerRevision = null;
                    TargetAcceptedContainerRevision = null;
                    RefreshTradeConfirmationStatusLocked(session);
                    return;
                }

                session.State = TradeState.Completing;
                var target = session.Target;
                var exchanged = false;
                try
                {
                    exchanged = TradeExchange.TryCompleteTrade(Character, SelfContainer, target, TargetContainer, _itemBuilder);
                }
                catch (InvalidOperationException)
                {
                    exchanged = false;
                }
                finally
                {
                    if (!exchanged && session.State == TradeState.Completing)
                    {
                        session.State = TradeState.Active;
                    }
                }

                if (!exchanged)
                {
                    CancelTradeSession(session, forceConservation: false);
                    return;
                }

                session.State = TradeState.Completed;
                Character.SendChatMessage("Accepted trade.");
                target.SendChatMessage("Accepted trade.");
                ResetTradeSessionLocked(session);
            }
        }

        private void ResetTradeSessionLocked(TradeSessionState session)
        {
            if (!ReferenceEquals(_tradeSession, session))
            {
                return;
            }

            var target = session.Target;
            TradeSession = false;
            _tradeSession = null;

            SelfContainer?.Clear(false);
            TargetContainer?.Clear(false);

            if (Character.Widgets.IntInputHandler == SelfIntInputHandler)
            {
                Character.Widgets.IntInputHandler = null;
            }

            if (target.Widgets.IntInputHandler == TargetIntInputHandler)
            {
                target.Widgets.IntInputHandler = null;
            }

            if (SelfInterface?.IsOpened == true)
            {
                Character.Widgets.CloseWidget(SelfInterface);
            }

            if (TargetInterface?.IsOpened == true)
            {
                target.Widgets.CloseWidget(TargetInterface);
            }

            if (SelfOverlay?.IsOpened == true)
            {
                Character.Widgets.CloseWidget(SelfOverlay);
            }

            if (TargetOverlay?.IsOpened == true)
            {
                target.Widgets.CloseWidget(TargetOverlay);
            }

            var targetScript = session.TargetScript;
            if (targetScript != null && ReferenceEquals(targetScript._linkedTradeSession, session))
            {
                targetScript._linkedTradeSession = null;
            }

            Target = null;
            SelfInterface = null;
            TargetInterface = null;
            SelfOverlay = null;
            TargetOverlay = null;
            SelfAccepted = false;
            TargetAccepted = false;
            SelfAcceptedContainerRevision = null;
            TargetAcceptedContainerRevision = null;
            SelfContainer = null;
            TargetContainer = null;
            SelfIntInputHandler = null;
            TargetIntInputHandler = null;
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
            public Action? CloseHandler { get; set; }

            public TradeInterfaceScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

            /// <summary>
            ///     Happens when this interface is opened.
            /// </summary>
            public override void OnOpen() { }

            /// <summary>
            ///     Happens when this interface is closed.
            /// </summary>
            public override void OnClose() => CloseHandler?.Invoke();
        }

        /// <summary>
        ///     Container for holding items in trade offer interfaces.
        /// </summary>
        public class TradeContainer : TradeItemContainer
        {
            /// <summary>
            ///     Contains last slots update.
            /// </summary>
            public HashSet<int> Updates { get; }

            /// <summary>
            ///     Gets the monotonically increasing content revision.
            /// </summary>
            public int Revision { get; private set; }

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
                Revision++;
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
