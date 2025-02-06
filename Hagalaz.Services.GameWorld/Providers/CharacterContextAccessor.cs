using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class CharacterContextAccessor : ICharacterContextAccessor, ICharacterContextProvider
    {
        public ICharacterContext Context { get; set; } = null!;
    }
}
