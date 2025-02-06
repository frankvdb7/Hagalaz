using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    /// <summary>
    /// </summary>
    public class StandardFamiliarDialogue : NpcDialogueScript
    {
        public StandardFamiliarDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Special Move", "Dismiss", "Nevermind");
                return true;
            });
            AttachDialogueOptionClickHandler("Special Move", (extraData1, extraData2) =>
            {
                if (Owner.FamiliarScript.Familiar == TalkingTo)
                {
                    Owner.FamiliarScript.SetSpecialMoveTarget(null);
                }

                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Dismiss", (extraData1, extraData2) =>
            {
                new FamiliarDismissEvent(Owner).Send();
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Nevermind", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
        }

        /// <summary>
        ///     Sets the source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void SetSource(IRuneObject? source)
        {
            if (!(source is INpc))
            {
                throw new Exception("Source is not an npc!");
            }

            if (!((source as INpc).Script is FamiliarScriptBase))
            {
                throw new Exception("Source is not a FamiliarScript!");
            }

            TalkingTo = source as INpc;
        }
    }
}