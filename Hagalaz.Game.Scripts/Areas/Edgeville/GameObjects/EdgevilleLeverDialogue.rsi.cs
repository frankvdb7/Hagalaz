using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{
    /// <summary>
    /// </summary>
    public class EdgevilleLeverDialogue : DialogueScript
    {
        /// <summary>
        ///     The obj.
        /// </summary>
        private IGameObject _obj;

        public EdgevilleLeverDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

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
        private void Setup()
        {
            AttachDialogueContinueClickHandlers();
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Mage bank", "Deserted keep (Wilderness)", "Green dragons east (Wilderness)");
                return true;
            });
            AttachDialogueOptionClickHandler("Mage bank", (extraData1, extraData2) =>
            {
                new LeverTeleport(_obj, 2539, 4716, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Deserted keep (Wilderness)", (extraData1, extraData2) =>
            {
                new LeverTeleport(_obj, 3153, 3923, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Green dragons east (Wilderness)", (extraData1, extraData2) =>
            {
                new LeverTeleport(_obj, 3315, 3697, 0, 0).PerformTeleport(Owner);
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
            if (source is not IGameObject gameObject)
            {
                throw new Exception("Source is not an gameobject!");
            }

            _obj = gameObject;
        }
    }
}