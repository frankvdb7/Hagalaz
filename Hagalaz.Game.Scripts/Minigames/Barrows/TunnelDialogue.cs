using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    public class TunnelDialogue : DialogueScript
    {
        public TunnelDialogue(ICharacterContextAccessor characterContextAccessor, BarrowsScript barrows, int barrowBrotherNpcID = 0) : base(characterContextAccessor)
        {
            _barrows = barrows;
            _barrowBrotherNpcID = barrowBrotherNpcID;
        }

        /// <summary>
        ///     The barrows script.
        /// </summary>
        private readonly BarrowsScript _barrows;

        /// <summary>
        ///     The barrow brother npc id.
        /// </summary>
        private int _barrowBrotherNpcID;

        /// <summary>
        ///     Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            AttachDialogueContinueClickHandlers();
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardDialogue("You've found a hidden tunnel, do you want to enter?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yeah I'm fearless!", "No way, that looks scary!");
                return true;
            });
            AttachDialogueOptionClickHandler("Yeah I'm fearless!", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                _barrows.TeleportToCrypts();
                return true;
            });
            AttachDialogueOptionClickHandler("No way, that looks scary!", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }
    }
}