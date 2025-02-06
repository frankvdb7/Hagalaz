using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    public interface IItemPartFactory
    {
        IItemPart Create(int itemId);
    }
}