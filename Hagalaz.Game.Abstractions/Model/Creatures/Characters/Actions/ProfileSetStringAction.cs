namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to set a string value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The string value to set.</param>
    public record ProfileSetStringAction(string Key, string Value);
}
