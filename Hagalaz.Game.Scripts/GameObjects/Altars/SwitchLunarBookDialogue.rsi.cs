using System;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    /// </summary>
    public class SwitchLunarBookDialogue : DialogueScript
    {
        /// <summary>
        ///     The obj.
        /// </summary>
        private IGameObject _obj = default!;
        private readonly IScopedGameMediator _gameMediator;

        public SwitchLunarBookDialogue(ICharacterContextAccessor contextAccessor, IScopedGameMediator gameMediator) : base(contextAccessor) => _gameMediator = gameMediator;

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
                StandardOptionDialogue("Change Spell Books?", "Yes, replace my spellbook.", "Nevermind.");
                return true;
            });

            AttachDialogueOptionClickHandler("Nevermind.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Yes, replace my spellbook.", (extraData1, extraData2) =>
            {
                var currentBook = Owner.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook);
                if (currentBook != MagicBook.LunarBook)
                {
                    StandardDialogue("Your mind clears and you switch to the lunar spellbook.");
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.MagicSettingsBook, MagicBook.LunarBook));
                }
                else
                {
                    StandardDialogue("Your mind clears and you switched to the normal spellbook.");
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.MagicSettingsBook, MagicBook.StandardBook));
                }

                return true;
            });

            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
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
            if (source is not IGameObject)
            {
                throw new Exception("");
            }

            _obj = (IGameObject)source;
        }
    }
}