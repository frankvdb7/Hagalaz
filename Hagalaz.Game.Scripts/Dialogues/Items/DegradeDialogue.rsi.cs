using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues.Items
{
    /// <summary>
    ///     Contains a dialogue when a character tries to wear a item that will degrade.
    /// </summary>
    public class DegradeDialogue : DialogueScript
    {
        /// <summary>
        ///     The item that should be degraded.
        /// </summary>
        private IItem _toDegrade;

        public DegradeDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            InterfaceInstance.DrawString(10, "Are you sure you want to equip this item?");
            InterfaceInstance.DrawString(29, "If you equip this item, it will degrade to a non-tradeable item.");
            InterfaceInstance.DrawString(13, _toDegrade.Name);
            InterfaceInstance.DrawItem(30, _toDegrade.Id, _toDegrade.Count);
            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (!_toDegrade.EquipmentScript.CanEquipItem(_toDegrade, Owner))
                {
                    return false;
                }

                Owner.Widgets.CloseChatboxOverlay();
                var slot = Owner.Equipment.GetInstanceSlot(_toDegrade);
                if (slot == EquipmentSlot.NoSlot)
                {
                    return false;
                }
                var itemBuilder = Owner.ServiceProvider.GetRequiredService<IItemBuilder>();
                Owner.Equipment.Replace(slot, itemBuilder.Create().WithId(_toDegrade.Id + 2).WithCount(_toDegrade.Count).Build());
                return true;

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

            _toDegrade = item;
        }
    }
}