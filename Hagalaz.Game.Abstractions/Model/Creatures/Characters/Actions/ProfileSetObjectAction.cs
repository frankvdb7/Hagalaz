namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to set a reference type (class) object in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Object">The object to set.</param>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    public record ProfileSetObjectAction<TObject>(string Key, TObject Object) where TObject : class;
}
