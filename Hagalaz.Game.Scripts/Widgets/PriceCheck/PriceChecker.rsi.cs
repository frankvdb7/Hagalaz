using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.PriceCheck
{
    /// <summary>
    ///     Represents price checker interface.
    /// </summary>
    public class PriceChecker : WidgetScript
    {
        /// <summary>
        ///     Price checker interface container.
        /// </summary>
        private class PriceCheckerInterfaceContainer : BaseItemContainer
        {
            /// <summary>
            ///     Contains owner of this class.
            /// </summary>
            private readonly ICharacter _owner;

            /// <summary>
            ///     Construct's new instance.
            /// </summary>
            /// <param name="owner"></param>
            public PriceCheckerInterfaceContainer(ICharacter owner) : base(StorageType.AlwaysStack, owner.Inventory.Capacity) => _owner = owner;

            /// <summary>
            ///     Happens when container is updated.
            /// </summary>
            /// <param name="slots"></param>
            public override void OnUpdate(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendItems(90, false, this, slots);
                RefreshPrices(slots);
            }

            /// <summary>
            ///     Calculate's total value of this container.
            /// </summary>
            /// <returns></returns>
            public long CalculateTotalValue()
            {
                long total = 0;
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


            /// <summary>
            ///     Refreshe's prices.
            /// </summary>
            /// <param name="slots"></param>
            public void RefreshPrices(HashSet<int>? slots = null)
            {
                _owner.Configurations.SendGlobalCs2Int(728, (int)CalculateTotalValue());

                if (slots == null)
                {
                    for (var i = 0; i < Capacity; i++)
                    {
                        if (this[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + i, this[i]!.ItemDefinition.TradeValue);
                        }
                        else
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + 1, 0);
                        }
                    }
                }
                else
                {
                    foreach (var i in slots)
                    {
                        if (this[i] != null)
                        {
                            _owner.Configurations.SendGlobalCs2Int((short)(700 + i), this[i]!.ItemDefinition.TradeValue);
                        }
                        else
                        {
                            _owner.Configurations.SendGlobalCs2Int(700 + 1, 0);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Contains inventory interface.
        /// </summary>
        private IWidget _inventoryInterface;

        /// <summary>
        ///     Contains price check interface container.
        /// </summary>
        private IItemContainer _priceCheckInterface;

        /// <summary>
        ///     Handler for adding X amount of items from the owner's inventory to the price check interface.
        /// </summary>
        private OnIntInput _inputHandler;

        public PriceChecker(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // open inventory overlay.
            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
            if (!Owner.Widgets.OpenInventoryOverlay(207, 1, defaultScript))
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            _inventoryInterface = Owner.Widgets.GetOpenWidget(207);
            if (_inventoryInterface == null)
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return;
            }

            // set options
            Owner.Configurations.SendCs2Script(150, [(207 << 16) | 0, 93, 4, 7, 0, -1, "Insert", "Insert-5", "Insert-10", "Insert-All", "Insert-X", "", "", "", ""
            ]);
            _inventoryInterface.SetOptions(0, 0, 27, 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x400); // allow clicking of 5 right click options + auto examine option ( last )
            InterfaceInstance.SetOptions(15, 0, 27, 0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x400);
            Owner.Configurations.SendGlobalCs2Int(729, 0);

            // price check interface & clear interface items (from previous price checks).
            _priceCheckInterface = new PriceCheckerInterfaceContainer(Owner);
            _priceCheckInterface.OnUpdate();


            // Component attachment for inventory (for ability to add items to price check interface).
            _inventoryInterface.AttachClickHandler(0, (componentID, clickType, itemID, slot) =>
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

                if (!item.ItemScript.CanTradeItem(item, Owner))
                {
                    Owner.SendChatMessage("You can't add this item to the pricechecker.");
                    return false;
                }

                var amount = 0;
                if (clickType == ComponentClickType.LeftClick)
                {
                    amount = 1;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    amount = 5;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    amount = 10;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    amount = Owner.Inventory.GetCount(item);
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    _inputHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        _inputHandler = Owner.Widgets.IntInputHandler = null;
                        if (value > 0)
                        {
                            AddItemToPriceChecker(item, value);
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Enter amount:");
                }
                else if (clickType == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                }

                if (amount > 0)
                {
                    AddItemToPriceChecker(item, amount);
                }

                return false;
            });

            // Component attachment for price checker (for ability to remove items to owner's inventory).
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, itemID, slot) =>
            {
                if (slot < 0 || slot >= _priceCheckInterface.Capacity)
                {
                    return false;
                }

                var item = _priceCheckInterface[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                var amount = 0;
                if (clickType == ComponentClickType.LeftClick)
                {
                    amount = 1;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    amount = 5;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    amount = 10;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    amount = _priceCheckInterface.GetCount(item);
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    _inputHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        _inputHandler = Owner.Widgets.IntInputHandler = null;
                        if (value > 0)
                        {
                            RemoveItemToInventory(item, value);
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Enter amount to remove:");
                }
                else if (clickType == ComponentClickType.Option10Click)
                {
                    Owner.SendChatMessage(item.ItemScript.GetExamine(item));
                }

                if (amount > 0)
                {
                    RemoveItemToInventory(item, amount);
                }

                return false;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_inventoryInterface != null)
            {
                Owner.Widgets.CloseWidget(_inventoryInterface);
            }

            Owner.Inventory.AddRange(_priceCheckInterface);
            _priceCheckInterface.Clear(false);
            _priceCheckInterface = null;
        }

        /// <summary>
        ///     Adds an item from the owner's inventory to the price checker interface.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="amount">The item amount.</param>
        /// <returns>
        ///     Returns true if successfully added to the price checker interface; false otherwise.
        /// </returns>
        private bool AddItemToPriceChecker(IItem item, int amount)
        {
            var toRemove = item.Clone();
            toRemove.Count = amount;
            var removed = Owner.Inventory.Remove(toRemove);
            if (removed > 0)
            {
                toRemove.Count = removed;
                _priceCheckInterface.Add(toRemove);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Removes an item from the price checker interface to the owner's inventory.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="amount">The item amount.</param>
        /// <returns>
        ///     Returns true if succesfully removed to the owner's inventory; false otherwise.
        /// </returns>
        private bool RemoveItemToInventory(IItem item, int amount)
        {
            var toRemove = item.Clone();
            toRemove.Count = amount;
            var removed = _priceCheckInterface.Remove(toRemove);
            if (removed > 0)
            {
                toRemove.Count = removed;
                Owner.Inventory.Add(toRemove);
                return true;
            }

            return false;
        }
    }
}