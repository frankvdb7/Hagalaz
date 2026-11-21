using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    ///     Spell book altar: selects <paramref name="targetBook"/>, or the standard book if it is already selected.
    /// </summary>
    public abstract class SwitchMagicBookDialogue(
        ICharacterContextAccessor contextAccessor,
        IScopedGameMediator gameMediator,
        MagicBook targetBook,
        string targetBookName) : SwitchBookDialogue(contextAccessor, gameMediator)
    {
        protected override string Question => "Change Spell Books?";

        protected override string ConfirmOption => "Yes, replace my spellbook.";

        protected override void SwitchBook()
        {
            var current = Owner.Profile.GetValue<MagicBook>(ProfileConstants.MagicSettingsBook);
            if (current != targetBook)
            {
                SwitchTo(ProfileConstants.MagicSettingsBook, targetBook, $"Your mind clears and you switch to the {targetBookName} spellbook.");
            }
            else
            {
                SwitchTo(ProfileConstants.MagicSettingsBook, MagicBook.StandardBook, "Your mind clears and you switched to the normal spellbook.");
            }
        }
    }
}
