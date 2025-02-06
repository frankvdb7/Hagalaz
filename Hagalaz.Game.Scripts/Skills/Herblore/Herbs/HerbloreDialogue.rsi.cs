using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    /// </summary>
    public class HerbloreDialogue : DialogueScript
    {
        public HerbloreDialogue(ICharacterContextAccessor characterContextAccessor, IItemService itemRepository, IHerbloreSkillService herbloreSkillService) : base(characterContextAccessor)
        {
            _itemRepository = itemRepository;
            _herbloreSkillService = herbloreSkillService;
        }

        /// <summary>
        ///     The definition.
        /// </summary>
        public HerbDto Definition { get; set; }

        /// <summary>
        ///     The current count.
        /// </summary>
        private int _currentCount;

        /// <summary>
        ///     The max count.
        /// </summary>
        private int _maxCount;

        /// <summary>
        ///     The tick delay.
        /// </summary>
        public int TickDelay { get; set; } = 1;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;
        private readonly IHerbloreSkillService _herbloreSkillService;

        /// <summary>
        ///     Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(8, 0, 0, 0); // unlocks all option.
            InterfaceInstance.DrawString(1, "Choose how many you wish to make, <br>then click on the item to begin.");
            Owner.Configurations.SendGlobalCs2Int(754, (int)InteractiveDialogueOptions.Make);

            var items = new [] {Definition.CleanHerbId}; // All items that can be created.

            for (var i = 0; i < 10; i++)
            {
                if (i >= items.Length)
                {
                    Owner.Configurations.SendGlobalCs2Int(i >= 6 ? 1139 + i - 6 : 755 + i, -1);
                    continue;
                }

                Owner.Configurations.SendGlobalCs2Int(i >= 6 ? 1139 + i - 6 : 755 + i, items[i]);

                Owner.Configurations.SendGlobalCs2String(i >= 6 ? 280 + i - 6 : 132 + i, _itemRepository.FindItemDefinitionById(items[i]).Name);
            }

            var count = Owner.Inventory.GetCountById(Definition.GrimyHerbId);
            SetMaxCount(count);
            SetCurrentCount(count, true);

            // count = 1
            InterfaceInstance.AttachClickHandler(5, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(1, false);
                return true;
            });

            // count = 5
            InterfaceInstance.AttachClickHandler(6, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(5, false);
                return true;
            });

            // count = 10
            InterfaceInstance.AttachClickHandler(7, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(10, false);
                return true;
            });

            // count = all
            InterfaceInstance.AttachClickHandler(8, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(_maxCount, false);
                return true;
            });

            // count += 1
            InterfaceInstance.AttachClickHandler(19, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(_currentCount + 1, false);
                return true;
            });

            // count -= 1
            InterfaceInstance.AttachClickHandler(20, (component, type, extraData1, slot) =>
            {
                SetCurrentCount(_currentCount - 1, false);
                return true;
            });

            OnComponentClick productClick = (component, type, extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                if (_currentCount > 0)
                {
                    _herbloreSkillService.QueueCleanHerbTask(Owner, Definition, _currentCount, TickDelay);
                    // int productIndex = component - 14;
                }

                return true;
            };

            var parent = Owner.Widgets.GetOpenWidget(InterfaceInstance.ParentId);
            for (var i = 0; i < items.Length; i++)
            {
                parent?.AttachClickHandler(i + 14, productClick);
            }
        }

        /// <summary>
        ///     Sets the max count.
        /// </summary>
        /// <param name="maxCount">The max count.</param>
        private void SetMaxCount(int maxCount)
        {
            _maxCount = maxCount;
            Owner.Configurations.SendBitConfiguration(8094, maxCount); // Max Quantity.
        }

        /// <summary>
        ///     Sets the current count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        private void SetCurrentCount(int count, bool refresh)
        {
            _currentCount = count;
            if (refresh)
            {
                Owner.Configurations.SendBitConfiguration(8095, count); // Curent Quantity.
            }
        }
    }
}