namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    public record ProfileDecrementIntAction(string Key, int Value, int MinValue = 0);
}
