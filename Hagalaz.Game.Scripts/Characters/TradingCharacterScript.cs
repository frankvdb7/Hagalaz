using System;
using System.Collections;
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
using Hagalaz.Game.Scripts.Items;

namespace Hagalaz.Game.Scripts.Characters
{
    /// <summary>
    ///     Character trading script.
    /// </summary>
    public class TradingCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly TradeExchange _tradeExchange;
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
            public TradeState State { get; private set; } = TradeState.Active;

            public TradeSessionState(TradingCharacterScript owner, ICharacter target)
            {
                Owner = owner;
                Target = target;
            }

            public void BeginCompletion()
            {
                if (State != TradeState.Active)
                    throw new InvalidOperationException("Only an active trade can begin completion.");

                State = TradeState.Completing;
            }

            public void MarkCompleted()
            {
                if (State != TradeState.Completing)
                    throw new InvalidOperationException("Only a completing trade can be marked completed.");

                State = TradeState.Completed;
            }

            public void MarkCancelled()
            {
                if (State != TradeState.Active)
                    throw new InvalidOperationException("Only an active trade can be marked cancelled.");

                State = TradeState.Cancelled;
            }

            public void ReturnToActive()
            {
                if (State != TradeState.Completing)
                    throw new InvalidOperationException("Only a completing trade can return to active.");

                State = TradeState.Active;
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
            _tradeExchange = new TradeExchange(itemBuilder);
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
                        target.Handle,
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
                if (!Character.Viewport.InBounds(LastRequest.Location))
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

            if (Character.Inventory.Items.FreeSlots != LastMyInventoryFreeSlots)
            {
                return true;
            }

            return target.Inventory.Items.FreeSlots != LastTargetInventoryFreeSlots;
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
                    HandleInventoryOfferClick(true, clickType, itemID, itemSlot));

            TargetOverlay.AttachClickHandler(0,
                (componentID, clickType, itemID, itemSlot) =>
                    HandleInventoryOfferClick(false, clickType, itemID, itemSlot));

            SelfInterface.AttachClickHandler(32,
                (componentID, clickType, itemID, itemSlot) =>
                    HandleOfferedItemClick(true, clickType, itemID, itemSlot));
            TargetInterface.AttachClickHandler(32,
                (componentID, clickType, itemID, itemSlot) =>
                    HandleOfferedItemClick(false, clickType, itemID, itemSlot));

            SelfInterface.AttachClickHandler(35,
                (componentID, clickType, itemID, itemSlot) =>
                    HandleOtherOfferClick(true, clickType, itemID, itemSlot));

            TargetInterface.AttachClickHandler(35,
                (componentID, clickType, itemID, itemSlot) =>
                    HandleOtherOfferClick(false, clickType, itemID, itemSlot));

            SelfInterface.AttachClickHandler(53,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    RequestTradeAmountInput(true,
                        Character.MoneyPouch.Examine + "<br>How many would you like to offer?",
                        amount => TryOfferMoney(true, amount));
                    return true;
                });

            TargetInterface.AttachClickHandler(53,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    RequestTradeAmountInput(false,
                        Target.MoneyPouch.Examine + "<br>How many would you like to offer?",
                        amount => TryOfferMoney(false, amount));
                    return true;
                });

            SelfInterface.AttachClickHandler(18,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(session, true);
                    return true;
                });
            TargetInterface.AttachClickHandler(18,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(session, false);
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


        private bool HandleInventoryOfferClick(bool self, ComponentClickType clickType, int itemID, int itemSlot)
        {
            var character = self ? Character : Target;
            var inventory = character.Inventory.Items;
            if (itemSlot < 0 || itemSlot >= inventory.Capacity) return false;

            var item = inventory[itemSlot];
            if (item == null || item.Id != itemID) return false;
            if (!item.ItemScript.CanTradeItem(item, character))
            {
                character.SendChatMessage("You can't trade this item.");
                return false;
            }

            if (clickType is ComponentClickType.LeftClick or ComponentClickType.Option2Click or
                ComponentClickType.Option3Click or ComponentClickType.Option4Click or ComponentClickType.Option5Click)
            {
                var max = inventory.GetCount(item);
                if (max <= 0) return false;

                if (clickType == ComponentClickType.Option5Click)
                {
                    RequestTradeAmountInput(self, "Please enter the amount to offer:",
                        amount => TryOfferInventoryItem(self, item, Math.Min(amount, max), -1));
                    return true;
                }

                var count = GetPresetAmount(clickType, max);
                if (count > 0 && !TryOfferInventoryItem(self, item, Math.Min(count, max), itemSlot)) return false;
            }
            else if (clickType == ComponentClickType.Option6Click)
            {
                character.SendChatMessage(GetMarketPriceMessage(item));
                return true;
            }
            else if (clickType == ComponentClickType.Option7Click)
            {
                character.SendChatMessage("Not yet implemented.");
                return true;
            }
            else if (clickType == ComponentClickType.Option10Click)
            {
                character.SendChatMessage(item.ItemScript.GetExamine(item));
                return true;
            }

            return true;
        }

        private bool HandleOfferedItemClick(bool self, ComponentClickType clickType, int itemID, int itemSlot)
        {
            var character = self ? Character : Target;
            var offer = (self ? SelfContainer : TargetContainer).Items;
            if (itemSlot < 0 || itemSlot >= offer.Capacity) return false;

            var item = offer[itemSlot];
            if (item == null || item.Id != itemID) return false;
            if (clickType is ComponentClickType.LeftClick or ComponentClickType.Option2Click or
                ComponentClickType.Option3Click or ComponentClickType.Option4Click or ComponentClickType.Option5Click)
            {
                var max = offer.GetCount(item);
                if (max <= 0) return false;

                if (clickType == ComponentClickType.Option5Click)
                {
                    RequestTradeAmountInput(self, "Please enter the amount to remove:",
                        amount => TryRemoveOfferedItem(self, item, Math.Min(amount, max), itemSlot));
                    return true;
                }

                var count = GetPresetAmount(clickType, max);
                if (count > 0 && !TryRemoveOfferedItem(self, item, Math.Min(count, max), itemSlot)) return false;
            }
            else if (clickType == ComponentClickType.Option6Click)
            {
                character.SendChatMessage(GetMarketPriceMessage(item));
                return true;
            }
            else if (clickType == ComponentClickType.Option10Click)
            {
                character.SendChatMessage(item.ItemScript.GetExamine(item));
                return true;
            }

            return true;
        }

        private bool HandleOtherOfferClick(bool self, ComponentClickType clickType, int itemID, int itemSlot)
        {
            var character = self ? Character : Target;
            var offer = (self ? TargetContainer : SelfContainer).Items;
            if (itemSlot < 0 || itemSlot >= offer.Capacity) return false;

            var item = offer[itemSlot];
            if (item == null || item.Id != itemID) return false;
            if (clickType == ComponentClickType.LeftClick)
            {
                character.SendChatMessage(GetMarketPriceMessage(item));
                return true;
            }

            if (clickType == ComponentClickType.Option10Click)
            {
                character.SendChatMessage(item.ItemScript.GetExamine(item));
                return true;
            }

            return true;
        }

        private static int GetPresetAmount(ComponentClickType clickType, int max) => clickType switch
        {
            ComponentClickType.LeftClick => 1,
            ComponentClickType.Option2Click => 5,
            ComponentClickType.Option3Click => 10,
            ComponentClickType.Option4Click => max,
            _ => 0
        };

        private void RequestTradeAmountInput(bool self, string message, Action<int> onAmount)
        {
            var character = self ? Character : Target;
            OnIntInput handler = null;
            handler = amount =>
            {
                if ((self ? SelfIntInputHandler : TargetIntInputHandler) != handler) return;
                character.Widgets.IntInputHandler = null;
                if (self) SelfIntInputHandler = null;
                else TargetIntInputHandler = null;
                if (amount > 0) onAmount(amount);
            };

            if (self) SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
            else TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
            character.Configurations.SendIntegerInput(message);
        }

        private static string GetMarketPriceMessage(IItem item)
        {
            var count = item.Count;
            var value = item.ItemDefinition.TradeValue;
            return count == 1
                ? item.Name + ": market price is " + (value == 1 ? "one coin." : value + " coins.")
                : item.Name + ": market price is " + value + " coins each (" + value * (long)count + " coins for " + count + ")";
        }

        private bool TryOfferInventoryItem(bool self, IItem item, int requestedCount, int preferredSlot)
        {
            return TryWithActiveTradeSession(self, (session, character, offer) =>
            {
                var count = Math.Min(requestedCount, character.Inventory.Items.GetCount(item));
                if (count <= 0) return false;
                if (!character.Inventory.Items.TryTransferTo(offer, item, count, preferredSlot)) return false;

                RefreshTradeOfferScreenLocked(session);
                ProcessTradeChangeLocked(session, self, false);
                return true;
            });
        }

        private bool TryRemoveOfferedItem(bool self, IItem item, int requestedCount, int preferredSlot)
        {
            return TryWithActiveTradeSession(self, (session, character, offer) =>
            {
                var count = Math.Min(requestedCount, offer.GetCount(item));
                if (count <= 0) return false;

                if (item.Id != 995)
                {
                    if (!offer.TryTransferTo(character.Inventory.Items, item, count, preferredSlot)) return false;

                    RefreshTradeOfferScreenLocked(session);
                    ProcessTradeChangeLocked(session, self, false);
                    return true;
                }

                var toRemove = item.Clone();
                toRemove.Count = count;

                if (!_tradeExchange.TryReturnMoneyToPouch(character, offer, toRemove, preferredSlot)) return false;

                RefreshTradeOfferScreenLocked(session);
                ProcessTradeChangeLocked(session, self, true);
                return true;
            });
        }

        private bool TryWithActiveTradeSession(bool self,
            Func<TradeSessionState, ICharacter, IItemContainer, bool> operation)
        {
            var session = _tradeSession;
            if (session == null) return false;

            lock (session.Gate)
            {
                if (!IsActiveSession(session)) return false;
                return operation(session, self ? Character : session.Target,
                    (self ? SelfContainer : TargetContainer).Items);
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
                var offer = (self ? SelfContainer : TargetContainer).Items;
                var coinOffer = _itemBuilder.Create().WithId(995).WithCount(requestedCount).Build();
                if (!offer.HasSpaceFor(coinOffer))
                {
                    return false;
                }

                if (_tradeExchange.TryOfferMoneyFromPouch(character, offer, coinOffer))
                {
                    RefreshTradeOfferScreenLocked(session);
                    ProcessTradeChangeLocked(session, self, false);
                    return true;
                }

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

                LastMyInventoryFreeSlots = Character.Inventory.Items.FreeSlots;
                LastTargetInventoryFreeSlots = session.Target.Inventory.Items.FreeSlots;
                Character.Configurations.SendGlobalCs2String(203,
                    "<br><br>" + session.Target.DisplayName + "<br>has " + LastTargetInventoryFreeSlots + " free<br>inventory slots.");
                session.Target.Configurations.SendGlobalCs2String(203,
                    "<br><br>" + Character.DisplayName + "<br>has " + LastMyInventoryFreeSlots + " free<br>inventory slots.");
            }
        }


        private void AcceptTrade(TradeSessionState session, bool self)
        {
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

        private void ResetTradeAcceptancesLocked()
        {
            SelfAccepted = false;
            TargetAccepted = false;
            SelfAcceptedContainerRevision = null;
            TargetAcceptedContainerRevision = null;
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

            Character.Configurations.SendItems(90, false, SelfContainer.Items, SelfContainer.Updates);
            Character.Configurations.SendItems(90, true, TargetContainer.Items, TargetContainer.Updates);
            Target.Configurations.SendItems(90, false, TargetContainer.Items, TargetContainer.Updates);
            Target.Configurations.SendItems(90, true, SelfContainer.Items, SelfContainer.Updates);

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

                    AcceptTrade(session, true);
                    return true;
                });

            TargetInterface.AttachClickHandler(21,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    AcceptTrade(session, false);
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

                try
                {
                    using (var transaction = ItemContainerTransaction.Begin(SelfContainer.Items,
                               TargetContainer.Items, Character.MoneyPouch, session.Target.MoneyPouch))
                    {
                        if (_tradeExchange.TryStageRefund(Character, SelfContainer.Items, session.Target, TargetContainer.Items))
                        {
                            session.MarkCancelled();
                            transaction.Commit();
                        }
                    }
                    if (session.State != TradeState.Cancelled && forceConservation)
                    {
                        var participants = new List<IItemTransactional>
                            { SelfContainer.Items, TargetContainer.Items };
                        if (Character.Rewards?.Items is { } firstRewards) participants.Add(firstRewards);
                        if (Character.Bank?.Items is { } firstBank) participants.Add(firstBank);
                        if (session.Target.Rewards?.Items is { } secondRewards) participants.Add(secondRewards);
                        if (session.Target.Bank?.Items is { } secondBank) participants.Add(secondBank);
                        using var transaction = ItemContainerTransaction.Begin(participants.ToArray());
                        if (_tradeExchange.TryStageEscrowRecovery(Character, SelfContainer.Items, session.Target, TargetContainer.Items))
                        {
                            session.MarkCancelled();
                            transaction.Commit();
                        }
                    }
                    if (session.State != TradeState.Cancelled)
                    {
                        ResetTradeAcceptancesLocked();
                        RefreshTradeConfirmationStatusLocked(session);
                    }
                }
                finally
                {
                    if (session.State == TradeState.Cancelled) ResetTradeSessionLocked(session);
                }
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
                    ResetTradeAcceptancesLocked();
                    RefreshTradeConfirmationStatusLocked(session);
                    return;
                }

                session.BeginCompletion();
                var target = session.Target;
                try
                {
                    using (var transaction = ItemContainerTransaction.Begin(SelfContainer.Items,
                               TargetContainer.Items, Character.MoneyPouch, target.MoneyPouch))
                    {
                        if (_tradeExchange.TryStageCompletion(Character, SelfContainer.Items, target, TargetContainer.Items))
                        {
                            session.MarkCompleted();
                            transaction.Commit();
                        }
                    }
                    if (session.State == TradeState.Completing)
                    {
                        session.ReturnToActive();
                        CancelTradeSession(session, forceConservation: false);
                        return;
                    }

                    Character.SendChatMessage("Accepted trade.");
                    target.SendChatMessage("Accepted trade.");
                }
                finally
                {
                    if (session.State == TradeState.Completed)
                    {
                        ResetTradeSessionLocked(session);
                    }
                    else if (session.State == TradeState.Completing)
                    {
                        session.ReturnToActive();
                    }
                }
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

            SelfContainer?.Items.Clear(false);
            TargetContainer?.Items.Clear(false);

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
        public class TradeContainer
        {
            public IItemContainer Items { get; }
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
            {
                Updates = [];
                Items = new ItemContainer(StorageType.Normal, 14, OnUpdate);
                OnUpdate();
            }

            /// <summary>
            ///     Happens when trade container get's updated.
            /// </summary>
            /// <param name="slots"></param>
            public void OnUpdate(HashSet<int>? slots = null)
            {
                Revision++;
                if (slots == null)
                {
                    Updates.Clear();
                    for (var i = 0; i < Items.Capacity; i++)
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
            public int CalculateTotalValue() => (int)ItemContainerTradeValue.Calculate(Items);
        }
    }
}
