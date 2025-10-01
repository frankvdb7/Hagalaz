using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Cache.Logic;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    /// <summary>
    /// </summary>
    public class PouchScreen : WidgetScript
    {
        public PouchScreen(
            ICharacterContextAccessor characterContextAccessor, IItemService itemService, ISummoningService summoningService,
            ISummoningSkillService summoningSkillService, IItemBuilder itemBuilder) : base(characterContextAccessor)
        {
            _itemService = itemService;
            _summoningService = summoningService;
            _summoningSkillService = summoningSkillService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Contains X unEquipHandler.
        /// </summary>
        private OnIntInput? _pouchXHandler;


        private readonly IItemService _itemService;
        private readonly ISummoningService _summoningService;
        private readonly ISummoningSkillService _summoningSkillService;
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(19,
                (componentID, type, itemID, slot) =>
                {
                    if (type != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    var scrollScreen = Owner.ServiceProvider.GetRequiredService<ScrollScreen>();
                    Owner.Widgets.OpenWidget(666, 0, scrollScreen, false);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(16,
                (componentID, type, itemID, itemSlot) =>
                {
                    if (itemSlot < 0)
                    {
                        return false;
                    }

                    var definition = _summoningService.FindDefinitionByPouchId(itemID).Result;
                    if (definition == null)
                    {
                        // The character either does not have the correct items to create the scroll or the scroll hasn't been added yet.
                        Owner.SendChatMessage("You don't have the required items to create this pouch.");
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
                        amount = 28;
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        _pouchXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _pouchXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }

                            if (!_summoningSkillService.CanCreatePouch(Owner, definition, itemID, value))
                            {
                                return;
                            }

                            var itemDef = _itemService.FindItemDefinitionById(itemID);
                            var requirements = ItemTypeLogic.GetCreateItemRequirements(itemDef);
                            foreach (var (key, iAmount) in requirements)
                            {
                                Owner.Inventory.Remove(_itemBuilder.Create().WithId(key).WithCount(iAmount * value).Build());
                            }

                            Owner.Inventory.Add(_itemBuilder.Create().WithId(itemID).WithCount(value).Build());
                            Owner.Statistics.AddExperience(StatisticsConstants.Summoning, definition.CreatePouchExperience * value);
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to infuse:");
                        return true;
                    }
                    else if (type == ComponentClickType.Option6Click)
                    {
                        Owner.SendChatMessage(_itemService.FindItemDefinitionById(itemID).Examine);
                    }

                    if (amount <= 0)
                    {
                        return true;
                    }

                    if (!_summoningSkillService.CanCreatePouch(Owner, definition, itemID, amount))
                    {
                        return true;
                    }

                    var itemDef = _itemService.FindItemDefinitionById(itemID);
                    var requirements = ItemTypeLogic.GetCreateItemRequirements(itemDef);
                    if (requirements != null)
                    {
                        foreach (var (key, iAmount) in requirements)
                        {
                            Owner.Inventory.Remove(_itemBuilder.Create().WithId(key).WithCount(iAmount * amount).Build());
                        }
                    }

                    Owner.Inventory.Add(_itemBuilder.Create().WithId(itemID).WithCount(amount).Build());
                    Owner.Statistics.AddExperience(StatisticsConstants.Summoning, definition.CreatePouchExperience * amount);

                    return true;
                });

            Owner.Configurations.SendCs2Script(757,
            [
                (672 << 16) | 16, 8, 10, "Infuse<col=FF9040>", "Infuse-5<col=FF9040>", "Infuse-10<col=FF9040>", "Infuse-All<col=FF9040>",
                "Infuse-X<col=FF9040>", "List<col=FF9040>", 1, 78
            ]);
            InterfaceInstance.SetOptions(16, 0, 462, 190);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() { }
    }
}