using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class SilverCastScreen : WidgetScript
    {
        private readonly ICraftingService _craftingService;

        /// <summary>
        ///     Contains tan X handler.
        /// </summary>
        private OnIntInput? _silverXHandler;
        
        public SilverCastScreen(ICharacterContextAccessor characterContextAccessor, ICraftingService craftingService) : base(characterContextAccessor) => _craftingService = craftingService;

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            foreach (var definition in _craftingService.FindAllSilver().Result)
            {
                if (!Owner.Inventory.Contains(definition.MouldID))
                {
                    InterfaceInstance.DrawItem(definition.ChildID, definition.MouldID, 1);
                    InterfaceInstance.SetVisible(definition.ChildID + 2, false); // disable make text
                    InterfaceInstance.SetVisible(definition.ChildID + 3, true); // enable requirement text
                }
                else
                {
                    InterfaceInstance.DrawItem(definition.ChildID, definition.ProductID, 1);
                    InterfaceInstance.AttachClickHandler(definition.ChildID, (componentID, type, extra1, extra2) => HandleClick(definition, type));
                }
            }
        }

        /// <summary>
        ///     Handles the click.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private bool HandleClick(SilverDto definition, ComponentClickType type)
        {
            if (Owner.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < definition.RequiredLevel)
            {
                Owner.SendChatMessage("You need a crafting level of " + definition.RequiredLevel + " to create that.");
                return false;
            }

            var count = 0;
            if (type == ComponentClickType.LeftClick)
            {
                count = 1;
            }
            else if (type == ComponentClickType.Option2Click)
            {
                count = 5;
            }
            else if (type == ComponentClickType.Option3Click)
            {
                count = Owner.Inventory.GetCountById(CraftingSkillService.SilverBar);
            }
            else if (type == ComponentClickType.Option4Click)
            {
                _silverXHandler = Owner.Widgets.IntInputHandler = value =>
                {
                    _silverXHandler = Owner.Widgets.IntInputHandler = null;
                    if (value <= 0)
                    {
                        Owner.SendChatMessage("Value can't be negative.");
                    }
                    else
                    {
                        Start(definition, count);
                    }
                };
                Owner.Configurations.SendIntegerInput("Please enter the amount to make:");
                return true;
            }

            Start(definition, count);
            return true;
        }

        /// <summary>
        ///     Starts the specified definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="count">The count.</param>
        private void Start(SilverDto definition, int count)
        {
            var task = Owner.ServiceProvider.GetRequiredService<SilverTask>();
            task.Definition = definition;
            task.TotalMakeCount = count;
            Owner.QueueTask(task);
            Owner.Widgets.CloseWidget(InterfaceInstance);
        }
    }
}