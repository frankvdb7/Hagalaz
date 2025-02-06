namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    public record ProfileSetObjectAction<TObject>(string Key, TObject Object) where TObject : class;
}
