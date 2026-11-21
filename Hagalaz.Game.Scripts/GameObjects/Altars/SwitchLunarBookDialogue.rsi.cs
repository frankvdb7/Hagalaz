using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    public class SwitchLunarBookDialogue(ICharacterContextAccessor contextAccessor, IScopedGameMediator gameMediator)
        : SwitchMagicBookDialogue(contextAccessor, gameMediator, MagicBook.LunarBook, "lunar")
    {
    }
}
