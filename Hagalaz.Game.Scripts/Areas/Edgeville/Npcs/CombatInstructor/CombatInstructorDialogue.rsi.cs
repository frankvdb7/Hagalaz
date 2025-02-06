using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.CombatInstructor
{
    /// <summary>
    /// </summary>
    public class CombatInstructorDialogue : NpcDialogueScript
    {
        public CombatInstructorDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.Movement.Unlock(false);

        /// <summary>
        ///     Called when [dialogue open].
        /// </summary>
        public override void OnOpen()
        {
            Owner.Movement.Lock(true);
            Setup();
        }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        private void Setup()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Greetings adventurer!", "I have some valuable information for you!");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "This place is the training dungeon.", "This dungeon will provide everything you need to become strong and fearsome!");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "That is great! Where can I start?");
                return true;
            });
            AttachDialogueContinueClickHandler(3, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You can start by equiping those training weapons you got.", "If you are in need of better equipment, then you need to go back to Edgeville.");
                return true;
            });
            AttachDialogueContinueClickHandler(4, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "How can I go back to Edgeville?");
                return true;
            });
            AttachDialogueContinueClickHandler(5, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You can use the two stairs located in this dungeon.", "One is located just south of here and one is located at the end of this dungeon.");
                return true;
            });
            AttachDialogueContinueClickHandler(6, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Ofcourse, if you do not feel like walking, then you can always use the home teleport located in your spellbook tab.");
                return true;
            });
            AttachDialogueContinueClickHandler(7, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "I have no more information for you at this moment.", "Feel free to kill these little goblin pests, they are starting to irritate me.");
                return true;
            });
            AttachDialogueContinueClickHandler(8, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Thank you very much!");
                return true;
            });
            AttachDialogueContinueClickHandler(9, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }

        /// <summary>
        ///     Determines whether this instance can be interrupted.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance can be interrupted; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanInterrupt() => false;
    }
}