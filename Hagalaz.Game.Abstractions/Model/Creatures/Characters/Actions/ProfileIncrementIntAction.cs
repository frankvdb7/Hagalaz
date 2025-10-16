namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions
{
    /// <summary>
    /// Represents an action to increment an integer value in the character's profile.
    /// </summary>
    /// <param name="Key">The profile key.</param>
    /// <param name="Value">The amount to increment.</param>
    /// <param name="MaxValue">The maximum value the integer can be.</param>
    public record ProfileIncrementIntAction(string Key, int Value, int MaxValue = int.MaxValue);
}
