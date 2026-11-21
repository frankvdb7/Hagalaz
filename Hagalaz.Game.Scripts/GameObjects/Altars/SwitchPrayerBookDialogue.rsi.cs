using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    ///     Prayer book altar: standard prayers switch to the ancient curses, anything else back to the standard prayers.
    /// </summary>
    public class SwitchPrayerBookDialogue(ICharacterContextAccessor contextAccessor, IScopedGameMediator gameMediator)
        : SwitchBookDialogue(contextAccessor, gameMediator)
    {
        protected override string Question => "Change Prayer Books?";

        protected override string ConfirmOption => "Yes, replace my prayer book.";

        protected override void SwitchBook()
        {
            var book = Owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
            if (book == PrayerBook.StandardBook)
            {
                SwitchTo(ProfileConstants.PrayerSettingsBook, PrayerBook.CursesBook, "Your prayers are heard by Zaros and you switched to the ancient curses.");
            }
            else
            {
                SwitchTo(ProfileConstants.PrayerSettingsBook, PrayerBook.StandardBook, "Your prayers are heard by the gods and you switched to the normal prayers.");
            }
        }
    }
}
