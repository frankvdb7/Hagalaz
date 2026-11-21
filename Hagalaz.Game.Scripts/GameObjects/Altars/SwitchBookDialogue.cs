using System;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    ///     The altar dialogue that asks whether to change spell book or prayer book, then switches it.
    ///     A book kind supplies the wording and which book to select.
    /// </summary>
    public abstract class SwitchBookDialogue : DialogueScript
    {
        private readonly IScopedGameMediator _gameMediator;

        protected SwitchBookDialogue(ICharacterContextAccessor contextAccessor, IScopedGameMediator gameMediator) : base(contextAccessor) =>
            _gameMediator = gameMediator;

        /// <summary>
        ///     The question shown above the options.
        /// </summary>
        protected abstract string Question { get; }

        /// <summary>
        ///     The option that confirms the change.
        /// </summary>
        protected abstract string ConfirmOption { get; }

        /// <summary>
        ///     Selects the book to switch to, based on the character's current book, by calling <see cref="SwitchTo"/>.
        /// </summary>
        protected abstract void SwitchBook();

        public override void OnClose()
        {
        }

        public override void OnOpen() => Setup();

        public override void SetSource(IRuneObject? source)
        {
            if (source is not IGameObject)
            {
                throw new Exception("");
            }
        }

        /// <summary>
        ///     Tells the character what happened and stores the chosen book in their profile.
        /// </summary>
        protected void SwitchTo(string profileKey, Enum book, string message)
        {
            StandardDialogue(message);
            _gameMediator.Publish(new ProfileSetEnumAction(profileKey, book));
        }

        private void Setup()
        {
            AttachDialogueContinueClickHandlers();
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardOptionDialogue(Question, ConfirmOption, "Nevermind.");
                return true;
            });

            AttachDialogueOptionClickHandler("Nevermind.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler(ConfirmOption, (extraData1, extraData2) =>
            {
                SwitchBook();
                return true;
            });

            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }
    }
}
