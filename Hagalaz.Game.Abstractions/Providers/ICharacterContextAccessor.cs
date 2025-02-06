using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Providers
{
    public interface ICharacterContextAccessor
    {
        ICharacterContext Context { get; }
    }
}
