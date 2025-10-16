namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Represents a delegate for handling a click event on a character option.
    /// This is triggered when a player right-clicks another character and selects an option from the context menu.
    /// </summary>
    /// <param name="target">The character that was clicked on.</param>
    /// <param name="forceRun">A value indicating whether the interacting character should force-run to the target.</param>
    public delegate void CharacterOptionClicked(ICharacter target, bool forceRun);
}
