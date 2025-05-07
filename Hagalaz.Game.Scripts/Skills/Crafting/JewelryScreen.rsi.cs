using System;
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
    public class JewelryScreen : WidgetScript
    {
        private readonly ICraftingService _craftingService;

        /// <summary>
        ///     Contains tan X handler.
        /// </summary>
        private OnIntInput? _jewelryXHandler;

        public JewelryScreen(ICharacterContextAccessor characterContextAccessor, ICraftingService craftingService) : base(characterContextAccessor) => _craftingService = craftingService;

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_jewelryXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _jewelryXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _jewelryXHandler = null;
            }
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            if (Owner.Inventory.Contains(CraftingSkillService.RingMoldId))
            {

                var rings = _craftingService.FindAllJewelry(JewelryDto.JewelryType.Ring).Result;
                InterfaceInstance.DrawString(98, string.Empty);
                foreach (var ring in rings)
                {
                    var ringId = CraftingSkillService.NoRingId;
                    if (Owner.Inventory.Contains(ring.ResourceID))
                    {
                        ringId = ring.ProductID;
                        InterfaceInstance.AttachClickHandler(ring.ChildID + 1, (componentID, type, extra1, extra2) => HandleClick(ring, type));
                    }

                    InterfaceInstance.DrawItem(ring.ChildID, ringId, 1);
                }
            }

            if (Owner.Inventory.Contains(CraftingSkillService.NecklaceMouldId))
            {
                var necklaces = _craftingService.FindAllJewelry(JewelryDto.JewelryType.Necklace).Result;
                InterfaceInstance.DrawString(22, string.Empty);
                foreach (var necklace in necklaces)
                {
                    var necklaceId = CraftingSkillService.NoNecklaceId;
                    if (Owner.Inventory.Contains(necklace.ResourceID))
                    {
                        necklaceId = necklace.ProductID;
                        InterfaceInstance.AttachClickHandler(necklace.ChildID + 1, (componentID, type, extra1, extra2) => HandleClick(necklace, type));
                    }

                    InterfaceInstance.DrawItem(necklace.ChildID, necklaceId, 1);
                }
            }

            if (Owner.Inventory.Contains(CraftingSkillService.AmuletMouldId))
            {
                var amulets = _craftingService.FindAllJewelry(JewelryDto.JewelryType.Amulet).Result;
                InterfaceInstance.DrawString(66, string.Empty);
                foreach(var amulet in amulets)
                {
                    var amuletId = CraftingSkillService.NoAmuletId;
                    if (Owner.Inventory.Contains(amulet.ResourceID))
                    {
                        amuletId = amulet.ProductID;
                        InterfaceInstance.AttachClickHandler(amulet.ChildID + 1, (componentID, type, extra1, extra2) => HandleClick(amulet, type));
                    }

                    InterfaceInstance.DrawItem(amulet.ChildID, amuletId, 1);
                }
            }

            if (Owner.Inventory.Contains(CraftingSkillService.BraceletMouldId))
            {
                var bracelets = _craftingService.FindAllJewelry(JewelryDto.JewelryType.Bracelet).Result;
                InterfaceInstance.DrawString(51, string.Empty);
                foreach (var bracelet in bracelets)
                {
                    var braceletId = CraftingSkillService.NoBraceletId;
                    if (Owner.Inventory.Contains(bracelet.ResourceID))
                    {
                        braceletId = bracelet.ProductID;
                        InterfaceInstance.AttachClickHandler(bracelet.ChildID + 1, (componentID, type, extra1, extra2) => HandleClick(bracelet, type));
                    }

                    InterfaceInstance.DrawItem(bracelet.ChildID, braceletId, 1);
                }
            }
        }

        /// <summary>
        ///     Handles the click.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private bool HandleClick(JewelryDto definition, ComponentClickType type)
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
                count = Math.Min(Owner.Inventory.GetCountById(definition.ResourceID), Owner.Inventory.GetCountById(CraftingSkillService.GoldBar));
            }
            else if (type == ComponentClickType.Option4Click)
            {
                _jewelryXHandler = Owner.Widgets.IntInputHandler = value =>
                {
                    _jewelryXHandler = Owner.Widgets.IntInputHandler = null;
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
        private void Start(JewelryDto definition, int count)
        {
            var task = Owner.ServiceProvider.GetRequiredService<JewelryTask>();
            task.Definition = definition;
            task.TotalMakeCount = count;
            Owner.QueueTask(task);
            Owner.Widgets.CloseWidget(InterfaceInstance);
        }
    }
}