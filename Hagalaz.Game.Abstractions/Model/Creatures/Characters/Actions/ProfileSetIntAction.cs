namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to set an integer value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The integer value to set.</param>
    public record ProfileSetIntAction(string Key, int Value);
}
