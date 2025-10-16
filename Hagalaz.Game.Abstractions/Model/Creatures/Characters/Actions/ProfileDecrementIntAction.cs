namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to decrement an integer value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The amount to decrement.</param>
    /// <param name="MinValue">The minimum value the integer can be.</param>
    public record ProfileDecrementIntAction(string Key, int Value, int MinValue = 0);
}
