using System;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Cooking
{
    [Obsolete("Use InteractiveDialogue instead.")]
    public class CookingDialogue : DialogueScript
    {
        public CookingDialogue(ICharacterContextAccessor characterContextAccessor, IItemService itemService) : base(characterContextAccessor)
        {
            _itemRepository = itemService;
        }

        /// <summary>
        ///     The obj
        /// </summary>
        public IGameObject Obj { get; set; }

        /// <summary>
        ///     The definition.
        /// </summary>
        public RawFoodDto Dto { get; set; }

        /// <summary>
        ///     The current count.
        /// </summary>
        private int _currentCount;

        /// <summary>
        ///     The max count.
        /// </summary>
        private int _maxCount;

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        /// <summary>
        ///     Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(8, 0, 0, 0); // unlocks all option.
            InterfaceInstance.DrawString(1, "Choose how many you wish to cook,<br>then click on the item to begin.");
            Owner.Configurations.SendGlobalCs2Int(754, (int)InteractiveDialogueOptions.Cook);

            // sendQuantitySelector
            //this.interfaceInstance.SetVisible(4, false);
            //this.interfaceInstance.SetVisible(9, false);

            var items = new [] {Dto.CookedItemId}; // All items that can be created.

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

            var count = Owner.Inventory.GetCountById(Dto.ItemId);
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

            var parent = Owner.Widgets.GetOpenWidget(InterfaceInstance.ParentId);
            parent?.AttachClickHandler(14, (component, type, extraData1, extraData2) =>
            {
                if (_currentCount > 0)
                {
                    Owner.QueueTask(new CookingTask(Owner, Obj, Dto, _currentCount));
                }

                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
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