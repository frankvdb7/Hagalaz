using System.Collections.Generic;
using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.EquipmentTab;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Widgets.Bank
{
    /// <summary>
    ///     Represents bank interface.
    /// </summary>
    public class BankScreen : WidgetScript
    {
        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget? _inventoryInterface;

        /// <summary>
        ///     Contains bank change unEquipHandler.
        /// </summary>
        private EventHappened? _bankChangeHandler;

        /// <summary>
        ///     Contains inventory change unEquipHandler.
        /// </summary>
        private EventHappened? _inventoryChangeHandler;

        /// <summary>
        ///     Contains boolean if noting is enabled.
        /// </summary>
        private bool _notingEnabled;

        /// <summary>
        ///     Contains bank tabs item counts.
        /// </summary>
        private int[] _bankTabsItemCount = ProfileConstants.BankSettingsTabDefault;

        private readonly IScopedGameMediator _mediator;
        private IGameConnectHandle? _bankXHandle;

        /// <summary>
        ///     Contains current tab id.
        /// </summary>
        private int _currentTabId = 8; // main tab

        /// <summary>
        ///     Contains bank X handler.
        /// </summary>
        private OnIntInput? _bankXHandler;

        public BankScreen(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator mediator) : base(characterContextAccessor) => _mediator = mediator;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            LoadTabsData();
            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(763, 1, defaultScript))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            _inventoryInterface = Owner.Widgets.GetOpenWidget(763);
            if (_inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            Setup();
        }


        /// <summary>
        ///     Loads tabs data.
        /// </summary>
        public void LoadTabsData() => _bankTabsItemCount = Owner.Profile.GetArray(ProfileConstants.BankSettingsTab, ProfileConstants.BankSettingsTabDefault).ToArray();

        /// <summary>
        ///     Set's configurations.
        /// </summary>
        public void Setup()
        {
            InterfaceInstance.SetOptions(95, 0, Owner.Bank.Capacity, 2622718);
            _inventoryInterface!.SetOptions(0, 0, 27, 2425982);
            Owner.Configurations.SendCs2Script(1451, []);

            _bankXHandle = _mediator.ConnectHandler<ProfileValueChanged<int>>(async (context) =>
            {
                if (context.Message.Key != ProfileConstants.BankSettingsOptionX)
                {
                    return;
                }

                RefreshBankX();
            });

            _bankChangeHandler = Owner.RegisterEventHandler(new EventHappened<BankChangedEvent>(e =>
            {
                RefreshBank(e.ChangedSlots);
                return true; // event handled
            }));
            _inventoryChangeHandler = Owner.RegisterEventHandler(new EventHappened<InventoryChangedEvent>(e =>
            {
                RefreshInventory(e.ChangedSlots);
                return true; // event handled
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
                    if (type == ComponentClickType.LeftClick)
                    {
                        amount = 1;
                    }
                    else if (type == ComponentClickType.Option2Click)
                    {
                        amount = 5;
                    }
                    else if (type == ComponentClickType.Option3Click)
                    {
                        amount = 10;
                    }
                    else if (type == ComponentClickType.Option4Click)
                    {
                        amount = Owner.Profile.GetValue(ProfileConstants.BankSettingsOptionX, ProfileConstants.BankSettingsOptionXDefault);
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        _bankXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _bankXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }
                            else
                            {
                                _mediator.Publish(new ProfileSetIntAction(ProfileConstants.BankSettingsOptionX, value));
                                Owner.Bank.DepositFromInventory(item, value, out var deposited);
                                if (deposited != null)
                                {
                                    var dslot = Owner.Bank.GetInstanceSlot(deposited);
                                    if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                                    {
                                        InsertIntoTab(_currentTabId, dslot);
                                    }
                                }
                            }
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to deposit:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option6Click)
                    {
                        amount = Owner.Inventory.GetCount(item);
                    }
                    else if (type == ComponentClickType.Option10Click)
                    {
                        Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    }
                    else
                    {
                        return false;
                    }

                    if (amount > 0)
                    {
                        Owner.Bank.DepositFromInventory(item, amount, out var deposit);
                        if (deposit != null)
                        {
                            var dslot = Owner.Bank.GetInstanceSlot(deposit);
                            if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                            {
                                InsertIntoTab(_currentTabId, dslot);
                            }
                        }
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(95,
                (componentID, type, itemID, slot) =>
                {
                    if (slot < 0 || slot >= Owner.Bank.Capacity)
                    {
                        return false;
                    }

                    var item = Owner.Bank[slot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    var amount = 0;
                    if (type == ComponentClickType.LeftClick)
                    {
                        amount = 1;
                    }
                    else if (type == ComponentClickType.Option2Click)
                    {
                        amount = 5;
                    }
                    else if (type == ComponentClickType.Option3Click)
                    {
                        amount = 10;
                    }
                    else if (type == ComponentClickType.Option4Click)
                    {
                        amount = Owner.Profile.GetValue(ProfileConstants.BankSettingsOptionX, ProfileConstants.BankSettingsOptionXDefault);
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        _bankXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _bankXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }

                            _mediator.Publish(new ProfileSetIntAction(ProfileConstants.BankSettingsOptionX, value));
                            Owner.Bank.WithdrawFromBank(item, value, _notingEnabled, out var withdrawed);
                            if (Owner.Bank.GetInstanceSlot(item) == -1)
                            {
                                RemoveFromTab(ItemTab(slot), slot);
                                RefreshTabs();
                            }
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to withdraw:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option6Click)
                    {
                        amount = Owner.Bank.GetCount(item);
                    }
                    else if (type == ComponentClickType.Option7Click)
                    {
                        amount = Owner.Bank.GetCount(item) - 1 >= 0 ? Owner.Bank.GetCount(item) - 1 : 0;
                    }
                    else if (type == ComponentClickType.Option10Click)
                    {
                        Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                    }
                    else
                    {
                        return false;
                    }

                    if (amount > 0)
                    {
                        Owner.Bank.WithdrawFromBank(item, amount, _notingEnabled, out var withdraw);
                        if (Owner.Bank.GetInstanceSlot(item) == -1)
                        {
                            RemoveFromTab(ItemTab(slot), slot);
                            RefreshTabs();
                        }
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(19,
                (componentID, type, extra1, extra2) =>
                {
                    _notingEnabled = !_notingEnabled;
                    RefreshSettings();
                    return true;
                });
            InterfaceInstance.AttachClickHandler(39,
                (componentID, type, extra1, extra2) =>
                {
                    if (!Owner.HasFamiliar())
                    {
                        return false;
                    }

                    if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                    {
                        return false;
                    }

                    var inventory = bob.Inventory;
                    foreach (var item in inventory)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (!Owner.Bank.DepositFromFamiliar(item, item.Count, out var outItem, inventory))
                        {
                            break;
                        }

                        if (outItem != null)
                        {
                            var dslot = Owner.Bank.GetInstanceSlot(outItem);
                            if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                            {
                                InsertIntoTab(_currentTabId, dslot);
                            }
                        }
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(33,
                (componentID, type, extra1, extra2) =>
                {
                    foreach (var item in Owner.Inventory.OfType<IItem>())
                    {
                        if (!Owner.Bank.DepositFromInventory(item, item.Count, out var outItem))
                        {
                            break;
                        }

                        var dslot = Owner.Bank.GetInstanceSlot(outItem);
                        if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                        {
                            InsertIntoTab(_currentTabId, dslot);
                        }
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(35,
                (componentID, type, extra1, extra2) =>
                {
                    if (!Owner.Bank.DepositFromMoneyPouch(out var outItem))
                    {
                        return false;
                    }

                    var dslot = Owner.Bank.GetInstanceSlot(outItem);
                    if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                    {
                        InsertIntoTab(_currentTabId, dslot);
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(37,
                (componentID, type, extra1, extra2) =>
                {
                    foreach (var item in Owner.Equipment)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (!Owner.Bank.DepositFromEquipment(item, item.Count, out var outItem))
                        {
                            break;
                        }

                        var dslot = Owner.Bank.GetInstanceSlot(outItem);
                        if (dslot == Owner.Bank.TakenSlots - 1 && _currentTabId != 8)
                        {
                            InsertIntoTab(_currentTabId, dslot);
                        }
                    }

                    return true;
                });
            _inventoryInterface.AttachDragHandler(0,
                (fromComponentID, fExtra, fromSlot, to, toComponentID, tExtra, toSlot) =>
                {
                    if (fromComponentID != 0 || toComponentID != 0)
                    {
                        return false;
                    }

                    if (to != _inventoryInterface)
                    {
                        return false;
                    }

                    if (fromSlot < 0 || fromSlot >= Owner.Inventory.Capacity)
                    {
                        return false;
                    }

                    if (toSlot < 0 || toSlot >= Owner.Inventory.Capacity)
                    {
                        return false;
                    }

                    Owner.Inventory.Swap(fromSlot, toSlot);
                    return true;
                });
            InterfaceInstance.AttachDragHandler(95, BankComponentDragged);

            InterfaceInstance.AttachClickHandler(119,
                (componentID, type, extra1, extra2) =>
                {
                    Owner.AddState(new BankingState());
                    var script = Owner.ServiceProvider.GetRequiredService<EquipmentWindow>();
                    Owner.Widgets.OpenWidget(667, 0, script, false);
                    return true;
                });

            int[] tabIDs = [64, 62, 60, 58, 56, 54, 52, 50, 48];

            bool TabClick(int componentID, ComponentClickType type, int extra1, int extra2)
            {
                if (type == ComponentClickType.LeftClick)
                {
                    _currentTabId = TabID(componentID);
                    RefreshTabs();
                    return true;
                }

                if (type == ComponentClickType.Option2Click)
                {
                    var tabID = TabID(componentID);
                    if (tabID >= 0 && tabID < 8)
                    {
                        Collapse(tabID);
                    }

                    RefreshTabs();
                    return true;
                }

                return false;
            }

            foreach (var t in tabIDs)
            {
                InterfaceInstance.AttachClickHandler(t, TabClick);
            }

            RefreshBank(null);
            RefreshInventory(null);
            RefreshBankX();
        }

        /// <summary>
        ///     Banks the component draged.
        /// </summary>
        /// <param name="fromComponentID">From component Id.</param>
        /// <param name="fromExtra1">From extra1.</param>
        /// <param name="fromExtra2">From extra2.</param>
        /// <param name="to">To.</param>
        /// <param name="toComponentID">To component Id.</param>
        /// <param name="toExtra1">To extra1.</param>
        /// <param name="toExtra2">To extra2.</param>
        /// <returns></returns>
        public bool BankComponentDragged(int fromComponentID, int fromExtra1, int fromExtra2, IWidget to, int toComponentID, int toExtra1, int toExtra2)
        {
            if (to != InterfaceInstance)
            {
                return false;
            }

            if (toComponentID == 95) // drag inside bank slots.
            {
                var fromSlot = fromExtra2;
                var toSlot = toExtra2;
                var fromID = fromExtra1;
                var toID = toExtra1;
                if (fromSlot < 0 || fromSlot >= Owner.Bank.Capacity)
                {
                    return false;
                }

                if (toSlot < 0 || toSlot >= Owner.Bank.Capacity)
                {
                    return false;
                }

                var fromItem = Owner.Bank[fromSlot];
                var toItem = Owner.Bank[toSlot];
                if (fromItem == null || toItem == null || fromItem.Id != fromID || toItem.Id != toID)
                {
                    return false;
                }

                Owner.Bank.Swap(fromSlot, toSlot);
                return true;
            }

            if (toComponentID >= 74 && toComponentID <= 82) // drag to the end of the column
            {
                var tabID = toComponentID - 74;
                if (tabID == 0)
                {
                    tabID = 8;
                }
                else
                {
                    tabID--;
                }

                if (tabID != 8 && _bankTabsItemCount[tabID] <= 0)
                {
                    return false;
                }

                var fromSlot = fromExtra2;
                var fromID = fromExtra1;
                if (fromSlot < 0 || fromSlot >= Owner.Bank.Capacity)
                {
                    return false;
                }

                var fromItem = Owner.Bank[fromSlot];
                if (fromItem == null || fromItem.Id != fromID)
                {
                    return false;
                }

                InsertIntoTab(tabID, fromSlot);
                return true;
            }

            if (TabID(toComponentID) != -1)
            {
                var fromSlot = fromExtra2;
                var fromID = fromExtra1;
                if (fromSlot < 0 || fromSlot >= Owner.Bank.Capacity)
                {
                    return false;
                }

                var fromItem = Owner.Bank[fromSlot];
                if (fromItem == null || fromItem.Id != fromID)
                {
                    return false;
                }

                InsertIntoTab(TabID(toComponentID), fromSlot);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get's tab write offset.
        /// </summary>
        /// <returns></returns>
        public int TabWriteOffset(int tab)
        {
            if (tab < 0 || tab > 7) // main tab.
            {
                return Owner.Bank.TakenSlots;
            }

            var offset = 0;
            for (var i = 0; i <= tab; i++)
            {
                offset += _bankTabsItemCount[i];
            }

            return offset;
        }

        /// <summary>
        ///     Get's tab write offset.
        /// </summary>
        /// <returns></returns>
        public int TabStartOffset(int tab)
        {
            if (tab < 0 || tab > 7) // main tab.
            {
                tab = 8;
            }

            var offset = 0;
            for (var i = 0; i < tab; i++)
            {
                offset += _bankTabsItemCount[i];
            }

            return offset;
        }

        /// <summary>
        ///     Get's tab in which item is.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public int ItemTab(int slot)
        {
            var total = 0;
            for (var tab = 0; tab < 8; tab++)
            {
                total += _bankTabsItemCount[tab];
                if (slot < total)
                {
                    return tab;
                }
            }

            return 8; // main
        }

        /// <summary>
        ///     Insert's item into given tab.
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public void InsertIntoTab(int tab, int slot)
        {
            RemoveFromTab(tab, slot);
            var offset = TabWriteOffset(tab);
            if (tab <= 7)
            {
                _bankTabsItemCount[tab]++;
            }

            Owner.Bank.Move(slot, offset);
            Owner.Bank.Sort();
        }

        /// <summary>
        ///     Remove's item from tab.
        /// </summary>
        /// <param name="tab">Id of the tab.</param>
        /// <param name="slot">Slot of the item.</param>
        public void RemoveFromTab(int tab, int slot)
        {
            var currentTab = ItemTab(slot);
            if (currentTab != 8)
            {
                _bankTabsItemCount[currentTab]--;
                if (_bankTabsItemCount[currentTab] <= 0)
                {
                    Collapse(currentTab);
                }
            }
        }

        /// <summary>
        ///     Collapses specific bank tab.
        /// </summary>
        /// <param name="tab"></param>
        public void Collapse(int tab)
        {
            if (tab > 0)
            {
                _bankTabsItemCount[tab - 1] += _bankTabsItemCount[tab];
            }

            _bankTabsItemCount[tab] = 0;
            for (var i = tab + 1; i < 8; i++)
            {
                _bankTabsItemCount[i - 1] = _bankTabsItemCount[i];
            }

            if (_bankTabsItemCount[tab] <= 0 && _currentTabId == tab)
            {
                _currentTabId = 8;
            }
        }

        /// <summary>
        ///     Refreshe's bank tabs settings.
        /// </summary>
        public void RefreshTabs()
        {
            var tabs = _bankTabsItemCount;
            var clientTabID = _currentTabId < 0 || _currentTabId > 7 ? 1 : _currentTabId + 2;
            Owner.Configurations.SendStandardConfiguration(1246, tabs[0] | (tabs[1] << 10) | (tabs[2] << 20));
            Owner.Configurations.SendStandardConfiguration(1247, tabs[3] | (tabs[4] << 10) | (tabs[5] << 20));
            Owner.Configurations.SendStandardConfiguration(1248, tabs[6] | (tabs[7] << 10) | (clientTabID << 27));
        }

        /// <summary>
        ///     Refreshe's bank settings.
        /// </summary>
        public void RefreshSettings()
        {
            Owner.Configurations.SendGlobalCs2Int(192, Owner.Bank.TakenSlots - 1);
            Owner.Configurations.SendStandardConfiguration(115, _notingEnabled ? 1 : 0);
        }

        /// <summary>
        ///     Get's Id of the tab by component Id.
        /// </summary>
        /// <param name="componentID"></param>
        /// <returns></returns>
        public static int TabID(int componentID) =>
            componentID switch
            {
                64 => 8, // main
                62 => 0, // first
                60 => 1, // second
                58 => 2, // third
                56 => 3, // fourth
                54 => 4, // fifth
                52 => 5, // sixth
                50 => 6, // seventh
                48 => 7, // nineth
                _ => -1,
            };


        /// <summary>
        ///     Refreshe's bank.
        /// </summary>
        public void RefreshBank(HashSet<int>? changedSlots = null)
        {
            Owner.Configurations.SendGlobalCs2Int(192, Owner.Bank.TakenSlots - 1);
            RefreshTabs();
            Owner.Configurations.SendItems(95, false, Owner.Bank, changedSlots);
        }

        /// <summary>
        ///     Refreshe's inventory.
        /// </summary>
        /// <param name="changedSlots"></param>
        public void RefreshInventory(HashSet<int>? changedSlots = null) => Owner.Configurations.SendItems(31, false, Owner.Inventory, changedSlots);

        public void RefreshBankX() =>
            Owner.Configurations.SendStandardConfiguration(1249,
                Owner.Profile.GetValue(ProfileConstants.BankSettingsOptionX, ProfileConstants.BankSettingsOptionXDefault));

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            _mediator.Publish(new ProfileSetObjectAction<int[]>(ProfileConstants.BankSettingsTab, _bankTabsItemCount));

            if (_inventoryInterface != null)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }

            if (_bankChangeHandler != null)
            {
                Owner.UnregisterEventHandler<BankChangedEvent>(_bankChangeHandler);
            }

            if (_inventoryChangeHandler != null)
            {
                Owner.UnregisterEventHandler<InventoryChangedEvent>(_inventoryChangeHandler);
            }

            if (_bankXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _bankXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _bankXHandler = null;
            }

            _bankXHandle?.Disconnect();
        }
    }
}