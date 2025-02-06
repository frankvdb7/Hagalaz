using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra
{
    /// <summary>
    /// </summary>
    public class AzzanadraDialogue : NpcDialogueScript
    {
        public AzzanadraDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [dialogue open].
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        private void Setup()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Greetings " + Owner.DisplayName + "!", "I can teleport you to various ancient places of power.");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Ancient Altar", "Lunar Altar", "The Temple at Senntisten", "Nevermind");
                return true;
            });
            AttachDialogueOptionClickHandler("Ancient Altar", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.AncientBook, 3233, 9315, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Lunar Altar", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.LunarBook, 2152, 3868, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("The Temple at Senntisten", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.AncientBook, 3182, 5713, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Nevermind", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }
    }
}