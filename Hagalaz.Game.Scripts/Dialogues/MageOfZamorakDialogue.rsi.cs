using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Dialogues
{
    /// <summary>
    /// </summary>
    public class MageOfZamorakDialogue : NpcDialogueScript
    {
        public MageOfZamorakDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.Angry, "What do you want?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Teleport me to the Abyss.", "Nevermind.");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Are you sure you want to go to the Abyss?");
                return true;
            });
            AttachDialogueContinueClickHandler(3, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes, I'm aware of the dangers.", "No, I am not prepared.");
                return true;
            });
            AttachDialogueContinueClickHandler(5, (extraData1, extraData2) =>
            {
                TalkingTo.Speak("Veniens! Sallakar! Rinnesset!");
                new StandardTeleportScript(MagicBook.StandardBook, 3039, 4830, 0, 0).PerformTeleport(Owner); // TODO - teleother anim / gfx
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Teleport me to the Abyss.", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Teleport me to the Abyss.");
                return true;
            });

            AttachDialogueOptionClickHandler("Nevermind.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("No, I am not prepared.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Yes, I'm aware of the dangers.", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Beam me up!");
                return true;
            });
        }
    }
}