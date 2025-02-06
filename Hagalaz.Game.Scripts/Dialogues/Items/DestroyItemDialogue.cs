using System;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues.Items
{
    /// <summary>
    ///     Contains destroy item script.
    /// </summary>
    public class DestroyItemDialogue : DialogueScript
    {
        private readonly IAudioBuilder _soundBuilder;

        /// <summary>
        ///     The item that should be destroyed.
        /// </summary>
        private IItem _toDestroy;

        public DestroyItemDialogue(ICharacterContextAccessor contextAccessor, IAudioBuilder soundBuilder) : base(contextAccessor) => _soundBuilder = soundBuilder;

        /// <summary>
        ///     Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            InterfaceInstance.DrawString(10, "Are you sure you want to destroy this object?");
            InterfaceInstance.DrawString(13, _toDestroy.Name);
            InterfaceInstance.DrawString(29, "If you destroy this object, you will have to earn it again.");
            InterfaceInstance.DrawItem(30, _toDestroy.Id, _toDestroy.Count);
            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.SpecialClick)
                {
                    return false;
                }

                var succes = false;
                var slot = Owner.Inventory.GetInstanceSlot(_toDestroy);
                if (slot == -1)
                {
                    return false;
                }

                var removed = Owner.Inventory.Remove(_toDestroy, slot);
                if (removed > 0)
                {
                    var sound = _soundBuilder.Create().AsSound().WithId(4500).Build();
                    Owner.Session.SendMessage(sound.ToMessage());
                    succes = true;
                }

                Owner.Widgets.CloseChatboxOverlay();
                return succes;
            });
            InterfaceInstance.AttachClickHandler(19, (componentID, clickType, extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }

        /// <summary>
        ///     Sets the source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void SetSource(IRuneObject? source)
        {
            if (source is not IItem item)
            {
                throw new Exception("Source is not an item!");
            }

            _toDestroy = item;
        }
    }
}