using System;
using System.Collections.Generic;
using System.Text;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class SmithingInterface : WidgetScript
    {
        public SmithingInterface(ICharacterContextAccessor contextAccessor, IItemService itemService) : base(contextAccessor)
        {
            _itemRepository = itemService;
        }

        /// <summary>
        ///     The definition.
        /// </summary>
        public SmithingDefinition Definition { get; set; }

        /// <summary>
        ///     Contains forge X handler.
        /// </summary>
        private OnIntInput? _forgeXHandler;

        /// <summary>
        ///     The entries; child as index
        /// </summary>
        private Dictionary<int, ForgingBarEntry> _entries = new();

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_forgeXHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _forgeXHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _forgeXHandler = null;
            }

            _entries = null!;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            try
            {
                SetItems();

                var clickHandlers = new OnComponentClick[4];
                clickHandlers[0] = (componentID, type, extraData1, extraData2) => // make 1 handler
                {
                    Owner.QueueTask(new ForgeTask(Owner, Definition, _entries[componentID], 1));
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                };
                clickHandlers[1] = (componentID, type, extraData1, extraData2) => // make 5 handler
                {
                    Owner.QueueTask(new ForgeTask(Owner, Definition, _entries[componentID], 5));
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                };
                clickHandlers[2] = (componentID, type, extraData1, extraData2) => // make X handler
                {
                    _forgeXHandler = Owner.Widgets.IntInputHandler = value =>
                    {
                        _forgeXHandler = Owner.Widgets.IntInputHandler = null;
                        if (value <= 0)
                        {
                            Owner.SendChatMessage("Value can't be negative.");
                        }
                        else
                        {
                            Owner.QueueTask(new ForgeTask(Owner, Definition, _entries[componentID], value));
                            Owner.Widgets.CloseWidget(InterfaceInstance);
                        }
                    };
                    Owner.Configurations.SendIntegerInput("Please enter the amount to smith:");
                    return true;
                };
                clickHandlers[3] = (componentID, type, extraData1, extraData2) => // make All handler
                {
                    // TODO - Calculate how many items can be made
                    Owner.QueueTask(new ForgeTask(Owner, Definition, _entries[componentID], 28));
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                };

                for (var i = 0; i < 4; i++)
                {
                    var b = (short)(24 - i);
                    for (var j = 0; j < Definition.ForgeDefinition.Entries.Length; j++)
                    {
                        InterfaceInstance.AttachClickHandler(b, clickHandlers[i]);
                        _entries.Add(b, Definition.ForgeDefinition.Entries[j]);
                        b += 8;
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                //Program.Logger.LogError(ex, "Something went wrong.");
            }
        }

        /// <summary>
        ///     Sets the items.
        /// </summary>
        private void SetItems()
        {
            short b = 17;
            for (var i = 0; i < Definition.ForgeDefinition.Entries.Length; i++)
            {
                var entry = Definition.ForgeDefinition.Entries[i];
                if (entry.Product.Id > 0)
                {
                    InterfaceInstance.SetVisible(b, true);
                    InterfaceInstance.DrawItem((short)(b + 1), entry.Product.Id, entry.Product.Count);
                    var strings = GetStrings(entry);
                    InterfaceInstance.DrawString((short)(b + 2), strings[0]);
                    InterfaceInstance.DrawString((short)(b + 3), strings[1]);
                }

                b += 8;
            }

            InterfaceInstance.DrawString(14, _itemRepository.FindItemDefinitionById(Definition.BarID).Name);
        }

        /// <summary>
        ///     Gets the strings.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        private string[] GetStrings(ForgingBarEntry entry)
        {
            var barString = new StringBuilder();
            var levelString = new StringBuilder();
            if (Owner.Inventory.Contains(Definition.BarID, entry.RequiredBarCount))
            {
                barString.Append("<col=00FF00>");
            }

            barString.Append(entry.RequiredBarCount);
            barString.Append(" ");
            barString.Append(entry.RequiredBarCount > 1 ? "Bars" : "Bar");

            if (Owner.Statistics.GetSkillLevel(StatisticsConstants.Smithing) >= entry.RequiredSmithingLevel)
            {
                levelString.Append("<col=FFFFFF>");
            }

            var itemNameArray = _itemRepository.FindItemDefinitionById(entry.Product.Id).Name.Split(' ')[1].ToCharArray();
            itemNameArray[0] = char.ToUpper(itemNameArray[0]);
            levelString.Append(itemNameArray);

            return [levelString.ToString(), barString.ToString()];
        }
    }
}