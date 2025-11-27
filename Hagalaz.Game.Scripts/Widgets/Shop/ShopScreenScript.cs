using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Utilities;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Widgets.Shop
{
    /// <summary>
    ///     Represents shop interface.
    /// </summary>
    public class ShopScreenScript : WidgetScript
    {
        public ShopScreenScript(
            ICharacterContextAccessor characterContextAccessor, IItemService itemService, IEventManager eventManager,
            IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor)
        {
            _itemRepository = itemService;
            _eventManager = eventManager;
            _widgetOptionBuilder = widgetOptionBuilder;
        }

        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget _inventoryInterface;

        /// <summary>
        ///     Contains inventory change unEquipHandler.
        /// </summary>
        private EventHappened _inventoryChangeHandler;

        /// <summary>
        ///     Contains inventory change unEquipHandler.
        /// </summary>
        private EventHappened _moneyChangeHandler;

        /// <summary>
        ///     The main stock change handler.
        /// </summary>
        private EventHappened _mainStockChangeHandler;

        /// <summary>
        ///     The sample stock change handler.
        /// </summary>
        private EventHappened _sampleStockChangeHandler;

        /// <summary>
        ///     The amount
        /// </summary>
        private int _count = 1;

        /// <summary>
        ///     The selected item
        /// </summary>
        private IItem? _selectedItem;

        /// <summary>
        ///     The previous screen
        /// </summary>
        private byte _currentScreen;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        private readonly IEventManager _eventManager;
        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(1266, 1, defaultScript))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            _inventoryInterface = Owner.Widgets.GetOpenWidget(1266);
            if (_inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            Setup();
        }

        /// <summary>
        ///     Set's configurations.
        /// </summary>
        public void Setup()
        {
            var containsSampleItems = Owner.CurrentShop!.SampleStockContainer.Any();
            Owner.Configurations.SendStandardConfiguration(118, 3); // cached container id, varies in capacity
            Owner.Configurations.SendStandardConfiguration(1496, containsSampleItems ? 553 : -1); // Sample stock container
            Owner.Configurations.SendStandardConfiguration(532, Owner.CurrentShop.CurrencyId); // currency
            if (containsSampleItems)
            {
                InterfaceInstance.SetOptions(21, 0, 12, _widgetOptionBuilder.SetRightClickOptions(9, true).Value); // Sample stock
            }

            InterfaceInstance.SetOptions(20,
                0,
                Owner.CurrentShop.MainStockContainer.Capacity * 6,
                _widgetOptionBuilder.SetRightClickOptions(9, true).Value);

            Owner.Configurations.SendCs2Script(149,
            [
                1266 << 16, 93, 4, 7, 1, -1, "Value", "Sell-1", "Sell-10", "Sell-50", "Sell-500"
            ]);
            _inventoryInterface.SetOptions(0, 0, 27, 1086); // Childid was 36

            RefreshCount();
            RefreshActiveScreen(containsSampleItems);

            //this.owner.CurrentShop.RefreshPrice(this.owner, null);

            Owner.Configurations.SendGlobalCs2String(361, Owner.CurrentShop.Name);

            RefreshMainStock(null);
            RefreshSampleStock(null);
            RefreshMoneyPouch();

            InterfaceInstance.SetVisible(52, Owner.CurrentShop.GeneralStore);

            _mainStockChangeHandler = _eventManager.Listen(new EventHappened<ShopStockChangedEvent>(e =>
            {
                if (e.Shop == Owner.CurrentShop)
                {
                    RefreshMainStock(e.ChangedSlots);
                }

                return false;
            }));
            _sampleStockChangeHandler = _eventManager.Listen(new EventHappened<ShopSampleStockChangedEvent>(e =>
            {
                if (e.Shop == Owner.CurrentShop)
                {
                    RefreshSampleStock(e.ChangedSlots);
                }

                return false;
            }));

            _inventoryChangeHandler = Owner.RegisterEventHandler(new EventHappened<InventoryChangedEvent>(e =>
            {
                RefreshInventory(e.ChangedSlots);
                return false;
            }));
            _moneyChangeHandler = Owner.RegisterEventHandler(new EventHappened<MoneyPouchChangedEvent>(e =>
            {
                RefreshMoneyPouch();
                return false;
            }));
            _inventoryInterface.AttachClickHandler(0,
                (componentID, type, itemID, slot) =>
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

                    var amount = 0;
                    switch (type)
                    {
                        case ComponentClickType.LeftClick:
                            Owner.SendChatMessage(item.ItemDefinition.Name + ": currently costs " +
                                                  StringUtilities.FormatNumber(Owner.CurrentShop.GetSellValue(item)) + " " +
                                                  _itemRepository.FindItemDefinitionById(Owner.CurrentShop.CurrencyId).Name.ToLower() + ".");
                            return true;
                        case ComponentClickType.Option2Click: amount = 1; break;
                        case ComponentClickType.Option3Click: amount = 10; break;
                        case ComponentClickType.Option4Click: amount = 50; break;
                        case ComponentClickType.Option5Click: amount = 500; break;
                        case ComponentClickType.Option10Click: Owner.SendChatMessage(item.ItemScript.GetExamine(item)); break;
                        default: return false;
                    }

                    if (amount > 0)
                    {
                        Owner.CurrentShop.MainStockContainer.SellFromInventory(Owner, item, amount);
                    }

                    return true;
                });

            bool BuyScreenHandler(int componentID, ComponentClickType type, int itemID, int slot)
            {
                if (slot < 0 || slot >= Owner.CurrentShop.MainStockContainer.Capacity)
                {
                    return false;
                }

                var item = Owner.CurrentShop.MainStockContainer[slot];
                if (item == null)
                {
                    return false;
                }

                var amount = 0;
                switch (type)
                {
                    case ComponentClickType.LeftClick:
                        RefreshItemInfo(item, slot, false);
                        return true;
                    case ComponentClickType.Option2Click: amount = 1; break;
                    case ComponentClickType.Option3Click: amount = 5; break;
                    case ComponentClickType.Option4Click: amount = 10; break;
                    case ComponentClickType.Option5Click: amount = 50; break;
                    case ComponentClickType.Option6Click: amount = 500; break;
                    case ComponentClickType.Option10Click: Owner.SendChatMessage(item.ItemScript.GetExamine(item)); break;
                    default: return false;
                }

                if (amount > 0)
                {
                    Owner.CurrentShop.MainStockContainer.BuyFromShop(Owner, item, amount);
                }

                return true;
            }

            bool SellScreenHandler(int componentID, ComponentClickType type, int itemID, int slot)
            {
                if (slot < 0 || slot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var item = Owner.Inventory[slot];
                if (item == null)
                {
                    return false;
                }

                var amount = 0;
                switch (type)
                {
                    case ComponentClickType.LeftClick:
                        RefreshItemInfo(item, slot, false);
                        return true;
                    case ComponentClickType.Option2Click: amount = 1; break;
                    case ComponentClickType.Option3Click: amount = 10; break;
                    case ComponentClickType.Option4Click: amount = 50; break;
                    case ComponentClickType.Option5Click: amount = 500; break;
                    case ComponentClickType.Option10Click: Owner.SendChatMessage(item.ItemScript.GetExamine(item)); break;
                    default: return false;
                }

                if (amount > 0)
                {
                    Owner.CurrentShop.MainStockContainer.SellFromInventory(Owner, item, amount);
                }

                return true;
            }

            InterfaceInstance.AttachClickHandler(20, Owner.HasState<ShopSellScreenState>() ? SellScreenHandler : BuyScreenHandler); // default handler

            InterfaceInstance.AttachClickHandler(21,
                (componentID, type, itemID, slot) =>
                {
                    if (slot < 0 || slot >= Owner.CurrentShop.SampleStockContainer.Capacity)
                    {
                        return false;
                    }

                    var item = Owner.CurrentShop.SampleStockContainer[slot];
                    if (item == null)
                    {
                        return false;
                    }

                    var amount = 0;
                    switch (type)
                    {
                        case ComponentClickType.LeftClick:
                            RefreshItemInfo(item, slot, true);
                            return true;
                        case ComponentClickType.Option2Click: amount = 1; break;
                        case ComponentClickType.Option3Click: amount = 5; break;
                        case ComponentClickType.Option4Click: amount = 10; break;
                        case ComponentClickType.Option5Click: amount = 50; break;
                        case ComponentClickType.Option6Click: amount = 500; break;
                        case ComponentClickType.Option10Click: Owner.SendChatMessage(item.ItemScript.GetExamine(item)); break;
                        default: return false;
                    }

                    if (amount > 0)
                    {
                        Owner.CurrentShop.SampleStockContainer.BuyFromShop(Owner, item, amount);
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(15,
                (componentID, type, itemID, slot) =>
                {
                    IncrementCount(1);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(208,
                (componentID, type, itemID, slot) =>
                {
                    IncrementCount(5);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(211,
                (componentID, type, itemID, slot) =>
                {
                    if (_selectedItem == null)
                    {
                        return false;
                    }
                    SetCount(_selectedItem.Count);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(214,
                (componentID, type, itemID, slot) =>
                {
                    DecrementCount(1);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(217,
                (componentID, type, itemID, slot) =>
                {
                    DecrementCount(5);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(220,
                (componentID, type, itemID, slot) =>
                {
                    SetCount(1);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(49,
                (componentID, type, itemID, slot) =>
                {
                    Owner.Configurations.SendStandardConfiguration(1078, -1);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(50,
                (componentID, type, itemID, slot) =>
                {
                    Owner.Configurations.SendStandardConfiguration(1078, 0);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(201,
                (componentID, type, itemID, slot) =>
                {
                    HandleBuyTakeSellButton();
                    return true;
                });

            InterfaceInstance.AttachClickHandler(28,
                (componentID, type, itemID, slot) =>
                {
                    InterfaceInstance.DetachClickHandlers(20);
                    InterfaceInstance.AttachClickHandler(20, BuyScreenHandler);
                    SetActiveScreen(0);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(29,
                (componentID, type, itemID, slot) =>
                {
                    InterfaceInstance.DetachClickHandlers(20);
                    InterfaceInstance.AttachClickHandler(20, SellScreenHandler);
                    SetActiveScreen(2);
                    return true;
                });
        }

        /// <summary>
        ///     Handles the buy take sell button.
        /// </summary>
        private void HandleBuyTakeSellButton()
        {
            if (_selectedItem == null)
            {
                return;
            }

            if (_currentScreen == 0)
            {
                Owner.CurrentShop!.MainStockContainer.BuyFromShop(Owner, _selectedItem, _count);
            }
            else if (_currentScreen == 1)
            {
                Owner.CurrentShop!.SampleStockContainer.BuyFromShop(Owner, _selectedItem, _count);
            }
            else if (_currentScreen == 2)
            {
                Owner.CurrentShop!.MainStockContainer.SellFromInventory(Owner, _selectedItem, _count);
            }
        }

        /// <summary>
        ///     Sets the information.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="sampleItem">if set to <c>true</c> [sample items].</param>
        private void RefreshItemInfo(IItem item, int slot, bool sampleItem)
        {
            _selectedItem = item;
            SetActiveScreen((byte)(Owner.HasState<ShopSellScreenState>() ? 2 : sampleItem ? 1 : 0));
            Owner.Configurations.SendStandardConfiguration(2562, item.Id);
            Owner.Configurations.SendStandardConfiguration(2563, slot);
            Owner.Configurations.SendGlobalCs2Int(1876, (int)item.EquipmentDefinition.Slot);
            Owner.Configurations.SendGlobalCs2String(362, item.ItemScript.GetExamine(item));
            if (sampleItem)
            {
                Owner.SendChatMessage(item.Name + ": free sample!");
            }
            else
            {
                Owner.SendChatMessage(item.Name + ": currently costs " +
                                      StringUtilities.FormatNumber(Owner.HasState<ShopSellScreenState>()
                                          ? Owner.CurrentShop!.GetSellValue(item)
                                          : Owner.CurrentShop!.GetBuyValue(item)) +
                                      " " + _itemRepository.FindItemDefinitionById(Owner.CurrentShop.CurrencyId).Name.ToLower() + ".");
            }

            SetCount(1);
        }

        /// <summary>
        ///     Increments the count.
        /// </summary>
        /// <param name="amount">The amount.</param>
        private void IncrementCount(int amount)
        {
            if ((uint)_count + (uint)amount > int.MaxValue)
            {
                SetCount(int.MaxValue);
            }
            else
            {
                SetCount(_count + amount);
            }
        }

        /// <summary>
        ///     Decrements the count.
        /// </summary>
        /// <param name="amount">The amount.</param>
        private void DecrementCount(int amount)
        {
            if (_count - amount < 1)
            {
                SetCount(1);
            }
            else
            {
                SetCount(_count - amount);
            }
        }

        /// <summary>
        ///     Sets the amount.
        /// </summary>
        /// <param name="count">The amount.</param>
        private void SetCount(int count)
        {
            _count = count;
            RefreshCount();
        }

        /// <summary>
        ///     Switches the active screen.
        /// </summary>
        private void SetActiveScreen(byte newScreen)
        {
            if (_currentScreen == newScreen)
            {
                return;
            }

            if (newScreen == 0 || newScreen == 1)
            {
                Owner.RemoveState<ShopSellScreenState>();
            }
            else
            {
                Owner.AddState(new ShopSellScreenState());
            }

            SetCount(1);
            RefreshActiveScreen(newScreen == 1);
            _currentScreen = newScreen;
        }

        /// <summary>
        ///     Refreshes the active screen.
        /// </summary>
        private void RefreshActiveScreen(bool sampleScreen) =>
            Owner.Configurations.SendStandardConfiguration(2561, Owner.HasState<ShopSellScreenState>() ? 93 : sampleScreen ? 553 : 3);

        /// <summary>
        ///     Refreshes the amount.
        /// </summary>
        private void RefreshCount() => Owner.Configurations.SendStandardConfiguration(2564, _count);

        /// <summary>
        ///     Refreshes the sample stock.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        private void RefreshSampleStock(HashSet<int>? changedSlots = null) => Owner.Configurations.SendItems(553, false, Owner.CurrentShop.SampleStockContainer, changedSlots);

        /// <summary>
        ///     Refreshes the main stock.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        private void RefreshMainStock(HashSet<int>? changedSlots = null) => Owner.Configurations.SendItems(3, false, Owner.CurrentShop.MainStockContainer, changedSlots);

        /// <summary>
        ///     Refreshes the inventory.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        private void RefreshInventory(HashSet<int>? changedSlots) => Owner.Configurations.SendItems(93, false, Owner.Inventory, changedSlots);

        /// <summary>
        ///     Refreshes the money pouch.
        /// </summary>
        private void RefreshMoneyPouch() => Owner.Configurations.SendItems(623, false, Owner.MoneyPouch);

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_inventoryInterface != null)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }

            if (_mainStockChangeHandler != null)
            {
                _eventManager.StopListen<ShopStockChangedEvent>(_mainStockChangeHandler);
            }

            if (_sampleStockChangeHandler != null)
            {
                _eventManager.StopListen<ShopSampleStockChangedEvent>(_sampleStockChangeHandler);
            }

            if (_inventoryChangeHandler != null)
            {
                Owner.UnregisterEventHandler<InventoryChangedEvent>(_inventoryChangeHandler);
            }

            if (_moneyChangeHandler != null)
            {
                Owner.UnregisterEventHandler<MoneyPouchChangedEvent>(_moneyChangeHandler);
            }

            if (Owner.CurrentShop != null)
            {
                _eventManager.SendEvent(new CloseShopEvent(Owner));
            }
        }
    }
}