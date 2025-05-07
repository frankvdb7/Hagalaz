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
    public class SwitchPrayerBookDialogue : DialogueScript
    {
        /// <summary>
        ///     The obj.
        /// </summary>
        private IGameObject _obj = default!;
        private readonly IScopedGameMediator _gameMediator;

        public SwitchPrayerBookDialogue(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor) => _gameMediator = gameMediator;

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
                StandardOptionDialogue("Change Prayer Books?", "Yes, replace my prayer book.", "Nevermind.");
                return true;
            });

            AttachDialogueOptionClickHandler("Nevermind.", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("Yes, replace my prayer book.", (extraData1, extraData2) =>
            {
                var book = Owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
                if (book == PrayerBook.StandardBook)
                {
                    StandardDialogue("Your prayers are heard by Zaros and you switched to the ancient curses.");
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.PrayerSettingsBook, PrayerBook.CursesBook));
                }
                else
                {
                    StandardDialogue("Your prayers are heard by the gods and you switched to the normal prayers.");
                    _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.PrayerSettingsBook, PrayerBook.StandardBook));
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