using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Slayer
{
    /// <summary>
    /// </summary>
    public class RewardsBuyScreen : WidgetScript
    {
        private readonly IScopedGameMediator _mediator;
        private readonly IItemBuilder _itemBuilder;

        public RewardsBuyScreen(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator mediator, IItemBuilder itemBuilder) : base(
            characterContextAccessor)
        {
            _mediator = mediator;
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
            InterfaceInstance.AttachClickHandler(24,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (Owner.Profile.TryGetValue<int>(ProfileConstants.SlayerRewardPoints, out var value) && value < 400)
                    {
                        Owner.SendChatMessage("You need 400 points in order to obtain this reward!");
                        return false;
                    }

                    _mediator.Publish(new ProfileDecrementIntAction(ProfileConstants.SlayerRewardPoints, 400));
                    Owner.Statistics.AddExperience(StatisticsConstants.Slayer, 10000);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(26,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (Owner.Profile.TryGetValue<int>(ProfileConstants.SlayerRewardPoints, out var value) && value < 75)
                    {
                        Owner.SendChatMessage("You need 75 points in order to obtain this reward!");
                        return false;
                    }

                    if (Owner.Inventory.Add(_itemBuilder.Create().WithId(13281).Build()))
                    {
                        _mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.SlayerRewardPoints, 75));
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(28,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (Owner.Profile.TryGetValue<int>(ProfileConstants.SlayerRewardPoints, out var value) && value < 35)
                    {
                        Owner.SendChatMessage("You need 35 points in order to obtain this reward!");
                        return false;
                    }

                    var deathRune = _itemBuilder.Create().WithId(560).Build();
                    var mindRune = _itemBuilder.Create().WithId(558).WithCount(4).Build();
                    if (Owner.Inventory.HasSpaceForRange([deathRune, mindRune]))
                    {
                        Owner.Inventory.Add(deathRune);
                        Owner.Inventory.Add(mindRune);
                        _mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.SlayerRewardPoints, 35));
                    }
                    else
                    {
                        Owner.SendChatMessage(GameStrings.InventoryFull);
                        return false;
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(37,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (Owner.Profile.TryGetValue<int>(ProfileConstants.SlayerRewardPoints, out var value) && value < 35)
                    {
                        Owner.SendChatMessage("You need 35 points in order to obtain this reward!");
                        return false;
                    }

                    if (Owner.Inventory.Add(_itemBuilder.Create().WithId(13280).WithCount(250).Build()))
                    {
                        _mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.SlayerRewardPoints, 35));
                    }

                    return true;
                });
            InterfaceInstance.AttachClickHandler(39,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (Owner.Profile.TryGetValue<int>(ProfileConstants.SlayerRewardPoints, out var value) && value < 35)
                    {
                        Owner.SendChatMessage("You need 35 points in order to obtain this reward!");
                        return false;
                    }

                    if (Owner.Inventory.Add(_itemBuilder.Create().WithId(4160).WithCount(250).Build()))
                    {
                        _mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.SlayerRewardPoints, 35));
                    }

                    return true;
                });
        }
    }
}