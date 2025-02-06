namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    public record ProfileIncrementIntAction(string Key, int Value, int MaxValue = int.MaxValue);
}
