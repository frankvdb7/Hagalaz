using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    public interface IWidgetCharacter
    {
        IWidgetId ForCharacter(ICharacter character);
    }
}