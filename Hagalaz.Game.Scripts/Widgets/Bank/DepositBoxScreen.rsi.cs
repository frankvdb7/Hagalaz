using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Bank
{
    /// <summary>
    /// </summary>
    public class DepositBoxScreen : WidgetScript
    {
        /// <summary>
        ///     Contains deposit X handler.
        /// </summary>
        private OnIntInput? _depositXHandler;

        public DepositBoxScreen(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.Configurations.SendCs2Script(150, [(11 << 16) | 17, 93, 6, 5, 0, "Deposit", "Deposit-5", "Deposit-10", "Deposit-X", "Deposit-All"]);
            InterfaceInstance.SetOptions(17, 0, 27, 1150);

            InterfaceInstance.AttachClickHandler(17,
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
                        _depositXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _depositXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }
                            else
                            {
                                Owner.Profile.SetValue(ProfileConstants.BankSettingsOptionX, value);
                                Owner.Bank.DepositFromInventory(item, value, out var deposited);
                            }
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to deposit:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option5Click)
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
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(18,
                (componentID, type, itemID, slot) =>
                {
                    for (short i = 0; i < Owner.Inventory.Capacity; i++)
                    {
                        var item = Owner.Inventory[i];
                        if (item != null)
                        {
                            if (!Owner.Bank.DepositFromInventory(item, item.Count, out var outItem))
                            {
                                break;
                            }
                        }
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(20,
                (componentID, type, itemID, slot) =>
                {
                    Owner.Bank.DepositFromMoneyPouch(out var outItem);
                    return true;
                });


            InterfaceInstance.AttachClickHandler(22,
                (componentID, type, itemID, slot) =>
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
                    }

                    return true;
                });

            InterfaceInstance.AttachClickHandler(24,
                (componentID, type, itemID, slot) =>
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
                    for (short i = 0; i < inventory.Capacity; i++)
                    {
                        var item = inventory[i];
                        if (item != null)
                        {
                            if (!Owner.Bank.DepositFromFamiliar(item, item.Count, out var outItem, inventory))
                            {
                                break;
                            }
                        }
                    }

                    return true;
                });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_depositXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _depositXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _depositXHandler = null;
            }
        }
    }
}