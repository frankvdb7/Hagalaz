using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.EquipmentTab;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents equipment tab.
    /// </summary>
    public class EquipmentTab : WidgetScript
    {
        /// <summary>
        ///     Contains equipment changes unEquipHandler.
        /// </summary>
        private EventHappened _handler;

        public EquipmentTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(38,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        if (Owner.IsBusy())
                        {
                            Owner.SendChatMessage("Please finish what you're doing before opening equipment interface.");
                            return true;
                        }

                        var equipmentWindow = Owner.ServiceProvider.GetRequiredService<EquipmentWindow>();
                        Owner.Widgets.OpenWidget(667, 0, equipmentWindow, true);
                    }

                    return false;
                });
            InterfaceInstance.AttachClickHandler(41,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        if (Owner.IsBusy())
                        {
                            Owner.SendChatMessage("Please finish what you're doing before opening the items kept on death interface.");
                            return true;
                        }

                        var itemsKeptOnDeathWindow = Owner.ServiceProvider.GetRequiredService<ItemsKeptOnDeathWindow>();
                        Owner.Widgets.OpenWidget(17, 0, itemsKeptOnDeathWindow, true);
                    }

                    return false;
                });
            InterfaceInstance.AttachClickHandler(42,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        if (Owner.IsBusy())
                        {
                            Owner.SendChatMessage("Please finish what you're doing before opening the toolbelt.");
                            return true;
                        }

                        var toolBeltWindow = Owner.ServiceProvider.GetRequiredService<ToolBeltWindow>();
                        Owner.Widgets.OpenWidget(1178, 0, toolBeltWindow, true);
                    }

                    return false;
                });
            InterfaceInstance.AttachClickHandler(43,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        if (Owner.IsBusy())
                        {
                            Owner.SendChatMessage("Please finish what you're doing before customizing your appearance.");
                            return true;
                        }

                        Owner.GetScript<WidgetsCharacterScript>()?.OpenCharacterDesignFrame();
                    }

                    return false;
                });

            OnComponentClick handler = (componentID, type, itemID, extra2) =>
            {
                var slot = GetSlot(componentID);
                if (slot == EquipmentSlot.NoSlot)
                {
                    return false;
                }

                var item = Owner.Equipment[slot];
                if (item == null || item.Id != itemID)
                {
                    return false;
                }

                item.ItemScript.ItemClickedInEquipment(type, item, Owner);
                return true;
            };
            int[] components = [6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 37, 46];
            foreach (var t in components)
            {
                InterfaceInstance.AttachClickHandler(t, handler);
            }

            _handler = Owner.RegisterEventHandler(new EventHappened<EquipmentChangedEvent>(e =>
            {
                RefreshEquipment(e.ChangedSlots);
                return false;
            }));

            RefreshEquipment(null);
        }

        /// <summary>
        ///     Refreshes the equipment.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        public void RefreshEquipment(HashSet<EquipmentSlot>? changedSlots = null)
        {
            Owner.Configurations.SendItems(94, false, Owner.Equipment, changedSlots?.Select(e => (int)e).ToHashSet());
        }


        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_handler != null)
            {
                Owner.UnregisterEventHandler<EquipmentChangedEvent>(_handler);
            }
        }

        /// <summary>
        ///     Get's slot Id from component Id.
        /// </summary>
        /// <param name="componentID"></param>
        /// <returns></returns>
        public static EquipmentSlot GetSlot(int componentID) =>
            componentID switch
            {
                6 => // hat
                    EquipmentSlot.Hat,
                9 => // cape
                    EquipmentSlot.Cape,
                12 => // amulet
                    EquipmentSlot.Amulet,
                15 => // weapon
                    EquipmentSlot.Weapon,
                18 => // chest
                    EquipmentSlot.Chest,
                21 => // shield
                    EquipmentSlot.Shield,
                24 => // legs
                    EquipmentSlot.Legs,
                27 => // gloves
                    EquipmentSlot.Hands,
                30 => // boots
                    EquipmentSlot.Feet,
                33 => // ring
                    EquipmentSlot.Ring,
                37 => // arrows
                    EquipmentSlot.Arrow,
                46 => // aura
                    EquipmentSlot.Aura,
                _ => EquipmentSlot.NoSlot
            };
    }
}