using System;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class RewardsBuyScreen : WidgetScript
    {
        private readonly IItemBuilder _itemBuilder;

        public RewardsBuyScreen(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder) : base(characterContextAccessor)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.DrawString(20, Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints).ToString());

            InterfaceInstance.AttachClickHandler(16,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    Owner.Widgets.OpenWidget(163, 0, Owner.ServiceProvider.GetRequiredService<RewardsLearnScreen>(), true);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(17,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    Owner.Widgets.OpenWidget(161, 0, Owner.ServiceProvider.GetRequiredService<RewardsAssignmentScreen>(), true);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(24, (componentID, clickType, extraData1, extraData2) =>
                TryPurchase(400, [], () => Owner.Statistics.AddExperience(StatisticsConstants.Slayer, 10000)));
            InterfaceInstance.AttachClickHandler(26, (componentID, clickType, extraData1, extraData2) =>
                TryPurchase(75, [_itemBuilder.Create().WithId(13281).Build()]));
            InterfaceInstance.AttachClickHandler(28, (componentID, clickType, extraData1, extraData2) =>
                TryPurchase(35, [_itemBuilder.Create().WithId(560).Build(), _itemBuilder.Create().WithId(558).WithCount(4).Build()]));
            InterfaceInstance.AttachClickHandler(37, (componentID, clickType, extraData1, extraData2) =>
                TryPurchase(35, [_itemBuilder.Create().WithId(13280).WithCount(250).Build()]));
            InterfaceInstance.AttachClickHandler(39, (componentID, clickType, extraData1, extraData2) =>
                TryPurchase(35, [_itemBuilder.Create().WithId(4160).WithCount(250).Build()]));
        }

        /// <summary>
        ///     The single owner of a reward purchase: check balance (absent key is zero), grant the whole reward with
        ///     one atomic inventory insertion, and debit only if that succeeded. Everything runs synchronously on the owning thread, so a repeated click sees the
        ///     already-debited balance and can neither mint points nor items.
        /// </summary>
        private bool TryPurchase(int cost, IReadOnlyCollection<IItem> items, Action? grant = null)
        {
            var balance = Owner.Profile.GetValue<int>(ProfileConstants.SlayerRewardPoints);
            if (balance < cost)
            {
                Owner.SendChatMessage($"You need {cost} points in order to obtain this reward!");
                return false;
            }

            if (items.Count > 0 && !Owner.Inventory.AddItems(items))
            {
                Owner.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            grant?.Invoke();
            Owner.Profile.SetValue(ProfileConstants.SlayerRewardPoints, balance - cost);
            InterfaceInstance.DrawString(20, (balance - cost).ToString());
            return true;
        }
    }
}
