using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Services.GameWorld.Providers
{
    public interface ICharacterContextProvider : ICharacterContextAccessor
    {
        public new ICharacterContext Context { get; set; }
    }
}
