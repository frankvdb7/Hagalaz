using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    /// <summary>
    /// </summary>
    public class ScrollScreen : WidgetScript
    {
        public ScrollScreen(
            ICharacterContextAccessor characterContextAccessor, IItemService itemService, ISummoningService summoningService,
            ISummoningSkillService summoningSkillService, IItemBuilder itemBuilder) : base(characterContextAccessor)
        {
            _itemService = itemService;
            _summoningService = summoningService;
            _summoningSkillService = summoningSkillService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     The amount of scrolls per pouch.
        /// </summary>
        private const short _scrollsPerPouch = 10;

        /// <summary>
        ///     Contains bank X unEquipHandler.
        /// </summary>
        private OnIntInput? _scrollXHandler;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemService;

        private readonly ISummoningService _summoningService;
        private readonly ISummoningSkillService _summoningSkillService;
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // 665 = inventory interface.

            InterfaceInstance.AttachClickHandler(18,
                (componentID, type, itemID, slot) =>
                {
                    if (type != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    var pouchScreen = Owner.ServiceProvider.GetRequiredService<PouchScreen>();
                    Owner.Widgets.OpenWidget(672, 0, pouchScreen, false);
                    return true;
                });

            InterfaceInstance.AttachClickHandler(16,
                (componentID, type, itemID, itemSlot) =>
                {
                    if (itemSlot < 0)
                    {
                        return false;
                    }

                    var definition = _summoningService.FindDefinitionByScrollId(itemID).Result;
                    if (definition == null)
                    {
                        // The character either does not have the correct items to create the scroll or the scroll hasn't been added yet.
                        Owner.SendChatMessage("You don't have the required pouch to create this scroll.");
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
                        amount = Owner.Inventory.GetCountById(definition.PouchId);
                    }
                    else if (type == ComponentClickType.Option5Click)
                    {
                        _scrollXHandler = Owner.Widgets.IntInputHandler = value =>
                        {
                            _scrollXHandler = Owner.Widgets.IntInputHandler = null;
                            if (value <= 0)
                            {
                                Owner.SendChatMessage("Value can't be negative.");
                            }

                            if (!_summoningSkillService.CanCreateScroll(Owner, definition))
                            {
                                return;
                            }

                            var removed = Owner.Inventory.Remove(_itemBuilder.Create().WithId(definition.PouchId).WithCount(value).Build());
                            if (removed <= 0)
                            {
                                return;
                            }

                            Owner.Inventory.Add(_itemBuilder.Create().WithId(definition.ScrollId).WithCount(removed * _scrollsPerPouch).Build());
                            Owner.Statistics.AddExperience(StatisticsConstants.Summoning, definition.ScrollExperience * removed);
                        };
                        Owner.Configurations.SendIntegerInput("Please enter the amount to transform:");
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

                    if (!_summoningSkillService.CanCreateScroll(Owner, definition))
                    {
                        return true;
                    }


                    var removed = Owner.Inventory.Remove(_itemBuilder.Create().WithId(definition.PouchId).WithCount(amount).Build());
                    if (removed <= 0)
                    {
                        return true;
                    }

                    Owner.Inventory.Add(_itemBuilder.Create().WithId(definition.ScrollId).WithCount(removed * _scrollsPerPouch).Build());
                    Owner.Statistics.AddExperience(StatisticsConstants.Summoning, definition.ScrollExperience * removed);

                    return true;
                });

            Owner.Configurations.SendCs2Script(763,
            [
                (666 << 16) | 16, 8, 10, "Transform<col=FF9040>", "Transform-5<col=FF9040>", "Transform-10<col=FF9040>", "Transform-All<col=FF9040>",
                "Transform-X<col=FF9040>", 1, 78
            ]);
            InterfaceInstance.SetOptions(16, 0, 462, 126);
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_scrollXHandler == null)
            {
                return;
            }

            if (Owner.Widgets.IntInputHandler == _scrollXHandler)
            {
                Owner.Widgets.IntInputHandler = null;
            }
        }
    }
}