using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Rewards
{
    /// <summary>
    /// </summary>
    public class RewardsInterface : WidgetScript
    {
        /// <summary>
        ///     The input handler.
        /// </summary>
        private OnIntInput? _rewardXHandler;

        /// <summary>
        ///     Contains rewards change handler.
        /// </summary>
        private EventHappened _rewardsChangeHandler;

        public RewardsInterface(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
            if (_rewardsChangeHandler != null)
            {
                Owner.UnregisterEventHandler<RewardsChangedEvent>(_rewardsChangeHandler);
            }

            if (_rewardXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _rewardXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _rewardXHandler = null;
            }
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            Owner.Configurations.SendCs2Script(150, [(InterfaceInstance.Id << 16) | 16, 90, 8, 35, 0, -1, "Claim", "Claim-X", "", "", "", "", "", "", ""]);
            InterfaceInstance.SetOptions(16, 0, (ushort)Owner.Rewards.Capacity, 0x2 | 0x4); // allow 2 options
            InterfaceInstance.DrawString(15, "Rewards Container");
            InterfaceInstance.DrawString(17, "Click or right click on an item to claim your reward!<br><col=FF0000>Warning: You can not refund an item once you claim it!</col>");
            InterfaceInstance.SetVisible(19, false); // disable the collect sprite
            if (Owner.Rewards.TakenSlots <= 48)
            {
                InterfaceInstance.SetVisible(18, false); // disable scroll bar
            }

            _rewardsChangeHandler = Owner.RegisterEventHandler(new EventHappened<RewardsChangedEvent>(e =>
            {
                RefreshRewards(e.ChangedSlots);
                return false;
            }));

            InterfaceInstance.AttachClickHandler(16, (componentID, clickType, itemID, slot) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (slot < 0 || slot >= Owner.Rewards.Capacity)
                    {
                        return false;
                    }

                    var item = Owner.Rewards[slot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    var removed = Owner.Rewards.Claim(item, 1);
                    if (removed > 0)
                    {
                        Owner.SendChatMessage("You claimed your <col=FF0000>" + removed + " x " + item.Name + "</col> reward. Thank you for your support!");
                        return true;
                    }

                    return false;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    _rewardXHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        Owner.Widgets.IntInputHandler = null;
                        if (!InterfaceInstance.IsOpened)
                        {
                            return;
                        }

                        if (value <= 0)
                        {
                            Owner.SendChatMessage("Amount must be greater than 0.");
                        }

                        var item = Owner.Rewards[slot];
                        if (item == null || item.Id != itemID)
                        {
                            return;
                        }

                        if (!item.ItemDefinition.Stackable && !item.ItemDefinition.Noted)
                        {
                            Owner.SendChatMessage("You can only do this on stackable items only!");
                            return;
                        }

                        var removed = Owner.Rewards.Claim(item, value);
                        if (removed > 0)
                        {
                            Owner.SendChatMessage("You claimed your <col=FF0000>" + removed + " x " + item.Name + "</col> reward. Thank you for your support!");
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Please enter the amount to claim:");
                    return true;
                }

                return false;
            });

            RefreshRewards(null);
        }

        /// <summary>
        ///     Refreshes the rewards.
        /// </summary>
        /// <param name="changedSlots">The changed slots.</param>
        public void RefreshRewards(HashSet<int>? changedSlots = null) => Owner.Configurations.SendItems(90, false, Owner.Rewards, changedSlots);
    }
}