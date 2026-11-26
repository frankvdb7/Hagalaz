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
            AttachDialogueContinueClickHandler(0, (_, _) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.Angry, "What do you want?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (_, _) =>
            {
                DefaultOptionDialogue("Teleport me to the Abyss.", "Nevermind.");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (_, _) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Are you sure you want to go to the Abyss?");
                return true;
            });
            AttachDialogueContinueClickHandler(3, (_, _) =>
            {
                DefaultOptionDialogue("Yes, I'm aware of the dangers.", "No, I am not prepared.");
                return true;
            });
            AttachDialogueContinueClickHandler(5, (_, _) =>
            {
                TalkingTo.Speak("Veniens! Sallakar! Rinnesset!");
                new StandardTeleportScript(MagicBook.StandardBook, 3039, 4830, 0, 0).PerformTeleport(Owner); // TODO - teleother anim / gfx
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Teleport me to the Abyss.", (_, _) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Teleport me to the Abyss.");
                return true;
            });

            AttachDialogueOptionClickHandler("Nevermind.", (_, _) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("No, I am not prepared.", (_, _) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Yes, I'm aware of the dangers.", (_, _) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Beam me up!");
                return true;
            });
        }
    }
}