using System.Linq;
using System.Text;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Utilities;

namespace Hagalaz.Game.Scripts.Widgets.EquipmentTab
{
    /// <summary>
    /// </summary>
    /// <seealso cref="WidgetScript" />
    public class ItemsKeptOnDeathWindow : WidgetScript
    {
        public ItemsKeptOnDeathWindow(ICharacterContextAccessor characterContextAccessor, IItemService itemService, IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor)
        {
            _itemRepository = itemService;
            _widgetOptionBuilder = widgetOptionBuilder;
        }

        /// <summary>
        ///     The wild button clicked
        /// </summary>
        private bool _wildButtonClicked;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            RefreshInfo(Owner.Area.IsPvP);
            InterfaceInstance.AttachClickHandler(17, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.Option10Click)
                {
                    var definition = _itemRepository.FindItemDefinitionById(extraData1);
                    Owner.SendChatMessage(definition.Name + ": is valued at " + StringUtilities.FormatNumber(definition.TradeValue) + ".");
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(18, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.Option10Click)
                {
                    var definition = _itemRepository.FindItemDefinitionById(extraData1);
                    Owner.SendChatMessage(definition.Name + ": is valued at " + StringUtilities.FormatNumber(definition.TradeValue) + ".");
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(20, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.Option10Click)
                {
                    var definition = _itemRepository.FindItemDefinitionById(extraData1);
                    Owner.SendChatMessage(definition.Name + ": is valued at " + StringUtilities.FormatNumber(definition.TradeValue) + ".");
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(22, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.Option10Click)
                {
                    var definition = _itemRepository.FindItemDefinitionById(extraData1);
                    Owner.SendChatMessage(definition.Name + ": is valued at " + StringUtilities.FormatNumber(definition.TradeValue) + ".");
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(28, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _wildButtonClicked = !_wildButtonClicked;
                    RefreshInfo(_wildButtonClicked);
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        ///     Refreshes the information.
        /// </summary>
        /// <param name="wilderniss">if set to <c>true</c> [wilderniss].</param>
        /// <param name="minigameMessage">The minigame message.</param>
        public void RefreshInfo(bool wilderniss, string? minigameMessage = null)
        {
            if (minigameMessage != null)
            {
                Owner.Configurations.SendBitConfiguration(9226, 2);
                Owner.Configurations.SendGlobalCs2String(351, minigameMessage);
            }
            else
            {
                if (Owner.HasFamiliar() && Owner.FamiliarScript is BobFamiliarScriptBase familiarScriptBase)
                {
                    Owner.Configurations.SendItems(530, false, familiarScriptBase.Inventory);
                }

                var itemSlotsOnDeathData = Owner.GetItemSlotsOnDeathData();
                var (droppedItems, keptItems) = Owner.GetItemsOnDeathData(itemSlotsOnDeathData);

                var i = 0;
                foreach (var slot in itemSlotsOnDeathData.keptItems)
                {
                    Owner.Configurations.SendBitConfiguration(9222 + i, slot);
                    i++;
                }

                Owner.Configurations.SendBitConfiguration(9227, i);

                Owner.Configurations.SendBitConfiguration(9226, wilderniss ? 1 : 0);
                Owner.Configurations.SendBitConfiguration(9229, Owner.HasState(StateType.DefaultSkulled) ? 1 : 0);

                var carriedWealth = droppedItems.Aggregate(0UL, (current, item) => current + ((ulong)item.ItemDefinition.TradeValue * (ulong)item.Count));
                var riskedWealth = keptItems.Aggregate(0UL, (current, item) => current + ((ulong)item.ItemDefinition.TradeValue * (ulong)item.Count));

                var builder = new StringBuilder();
                builder.Append("The number of items kept on death is normally 3.");
                builder.Append("<br>").Append("<br>");
                builder.Append("The maximum this can be boosted to is 5.");
                builder.Append("<br>").Append("<br>");
                builder.Append("Carried wealth:").Append("<br>").Append(carriedWealth > int.MaxValue ? "Too high!" : StringUtilities.FormatNumber((int)carriedWealth));
                builder.Append("<br>").Append("<br>");
                builder.Append("Risked wealth:").Append("<br>").Append(riskedWealth > int.MaxValue ? "Too high!" : StringUtilities.FormatNumber((int)riskedWealth));
                builder.Append("<br>").Append("<br>");
                builder.Append("Current hub: Edgeville");
                if (builder.Length > 0)
                {
                    Owner.Configurations.SendGlobalCs2String(352, builder.ToString());
                }

                InterfaceInstance.SetOptions(18, 0, 14, _widgetOptionBuilder.SetRightClickOption(9, true).Value); // highlighted items
                InterfaceInstance.SetOptions(17, 1, 47, _widgetOptionBuilder.SetRightClickOption(9, true).Value); // non-highlighted items
                InterfaceInstance.SetOptions(20, 1, 47, _widgetOptionBuilder.SetRightClickOption(9, true).Value); // special items
                InterfaceInstance.SetOptions(22, 1, 47, _widgetOptionBuilder.SetRightClickOption(9, true).Value);
            }
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}