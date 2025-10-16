namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to set a boolean value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The boolean value to set.</param>
    public record ProfileSetBoolAction(string Key, bool Value);
}
