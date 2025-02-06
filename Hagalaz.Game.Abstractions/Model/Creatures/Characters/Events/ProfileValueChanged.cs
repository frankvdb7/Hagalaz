namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events
{
    public record ProfileValueChanged<T>(string Key, T NewValue, T? OldValue);
}
