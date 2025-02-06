namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Contains delegate for character option click.
    /// Happens when character clicks other character.
    /// </summary>
    /// <param name="target">Character that was clicked.</param>
    /// <param name="forceRun">Wheter character should force run to target.</param>
    public delegate void CharacterOptionClicked(ICharacter target, bool forceRun);
}
