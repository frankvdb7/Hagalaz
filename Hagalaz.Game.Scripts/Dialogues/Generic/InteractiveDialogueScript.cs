using System;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues.Generic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="WidgetScript" />
    public class InteractiveDialogueScript : WidgetScript
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedItemID">The item identifier.</param>
        /// <param name="currentCount">The current count.</param>
        /// <returns></returns>
        public delegate bool PerformMakeProduct(int selectedItemID, int currentCount);

        /// <summary>
        /// Opens the interactive dialogue.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="script">The interactive dialogue script.</param>
        /// <returns></returns>
        public static bool OpenInteractiveDialogue(ICharacter character, InteractiveDialogueScript script)
        {
            var dialogScript = character.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
            character.Widgets.OpenChatboxOverlay((int)DialogueInterfaces.InteractiveChatBox, 0, dialogScript, false);
            var parent = character.Widgets.GetOpenWidget((int)DialogueInterfaces.InteractiveChatBox);
            return parent != null &&
                   character.Widgets.OpenWidget((int)DialogueInterfaces.InteractiveSelectAmountBox, parent, 4, 0, script, false);
        }

        /// <summary>
        /// The current count.
        /// </summary>
        private int _currentCount;

        /// <summary>
        /// The maximum count.
        /// </summary>
        private int _maxCount;

        /// <summary>
        /// The products.
        /// </summary>
        public int[] ProductIds { get; set; }

        /// <summary>
        /// The perform make product callback.
        /// </summary>
        public PerformMakeProduct PerformMakeProductCallback { get; set; }

        /// <summary>
        /// The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        /// Gets or sets the information.
        /// </summary>
        /// <value>
        /// The information.
        /// </value>
        public string Info { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public InteractiveDialogueOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the product naming callback.
        /// </summary>
        /// <value>
        /// The product naming callback.
        /// </value>
        public Func<int, string> ProductNamingCallback { get; set; }

        public InteractiveDialogueScript(
            ICharacterContextAccessor characterContextAccessor, IItemService itemService,
            IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor)
        {
            _itemRepository = itemService;
            _widgetOptionBuilder = widgetOptionBuilder;
        }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(13, 0, 0, _widgetOptionBuilder.SetLeftClickOption(true).Value); // unlocks all option.
            InterfaceInstance.DrawString(6, Info ?? DialogueConstants.DefaultInfoString);
            Owner.Configurations.SendGlobalCs2Int(754, (int)Options);

            for (var i = 0; i < 10; i++)
            {
                if (i >= ProductIds.Length)
                {
                    Owner.Configurations.SendGlobalCs2Int(i >= 6 ? 1139 + i - 6 : 755 + i, -1);
                    continue;
                }

                Owner.Configurations.SendGlobalCs2Int(i >= 6 ? 1139 + i - 6 : 755 + i, ProductIds[i]);

                var name = ProductNamingCallback != null
                    ? ProductNamingCallback.Invoke(ProductIds[i])
                    : _itemRepository.FindItemDefinitionById(ProductIds[i]).Name;
                Owner.Configurations.SendGlobalCs2String(i >= 6 ? 280 + i - 6 : 132 + i, name);
            }

            // count = 1
            InterfaceInstance.AttachClickHandler(10,
                (component, type, extraData1, slot) =>
                {
                    SetCurrentCount(1, false);
                    return true;
                });

            // count = 5
            InterfaceInstance.AttachClickHandler(11,
                (component, type, extraData1, slot) =>
                {
                    SetCurrentCount(5, false);
                    return true;
                });

            // count = 10
            InterfaceInstance.AttachClickHandler(12,
                (component, type, extraData1, slot) =>
                {
                    SetCurrentCount(10, false);
                    return true;
                });

            // count = all
            InterfaceInstance.AttachClickHandler(13,
                (component, type, extraData1, slot) =>
                {
                    SetCurrentCount(_maxCount, false);
                    return true;
                });

            // count += 1
            InterfaceInstance.AttachClickHandler(24,
                (component, type, extraData1, slot) =>
                {
                    IncreaseCurrentCount(1, false);
                    return true;
                });

            // count -= 1
            InterfaceInstance.AttachClickHandler(25,
                (component, type, extraData1, slot) =>
                {
                    DecreaseCurrentCount(1, false);
                    return true;
                });

            OnComponentClick productClick = (component, type, extraData1, extraData2) =>
            {
                if (type == ComponentClickType.SpecialClick)
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return PerformMakeProductCallback(GetItemIDForComponentID(component), _currentCount);
                }

                return false;
            };

            var parent = Owner.Widgets.GetOpenWidget(InterfaceInstance.ParentId);
            for (var i = 0; i < ProductIds.Length; i++)
            {
                parent?.AttachClickHandler(i + 14, productClick);
            }

            RefreshCurrentCount();
            RefreshMaxCount();
        }

        /// <summary>
        /// Gets the item identifier for component identifier.
        /// </summary>
        /// <param name="componentID">The component identifier.</param>
        /// <returns></returns>
        private int GetItemIDForComponentID(int componentID)
        {
            int index;
            if (componentID == 26)
                index = 7;
            else if (componentID == 21)
                index = 8;
            else if (componentID == 22)
                index = 9;
            else
                index = componentID - 14;
            return ProductIds[index];
        }

        /// <summary>
        /// Sets the max count.
        /// </summary>
        /// <param name="maxCount">The max count.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        public virtual void SetMaxCount(int maxCount, bool refresh)
        {
            _maxCount = maxCount;
            if (refresh) RefreshMaxCount();
        }

        /// <summary>
        /// Refreshes the maximum count.
        /// </summary>
        private void RefreshMaxCount() => Owner.Configurations.SendBitConfiguration(8094, _maxCount);

        /// <summary>
        /// Sets the current count.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        public virtual void SetCurrentCount(int count, bool refresh)
        {
            _currentCount = count;
            if (refresh) RefreshCurrentCount();
        }

        /// <summary>
        /// Refreshes the current count.
        /// </summary>
        private void RefreshCurrentCount() => Owner.Configurations.SendBitConfiguration(8095, _currentCount);

        /// <summary>
        /// Increments the current count.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        protected virtual void IncreaseCurrentCount(int amount, bool refresh)
        {
            if (_currentCount + amount >= _maxCount) amount = _maxCount - _currentCount;
            if (amount < 0) amount = 0;
            SetCurrentCount(amount, refresh);
        }

        /// <summary>
        /// Decreases the current count.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        protected virtual void DecreaseCurrentCount(int amount, bool refresh)
        {
            if (amount > _currentCount) amount = _currentCount;
            SetCurrentCount(_currentCount - amount, refresh);
        }
    }
}