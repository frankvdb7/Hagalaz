namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Defines the contract for a script that controls the logic and flow of a dialogue.
    /// </summary>
    public interface IDialogueScript : IWidgetScript
    {
        /// <summary>
        /// Sets the source entity (e.g., an NPC or object) that the character interacted with to start this dialogue.
        /// </summary>
        /// <param name="source">The source entity.</param>
        void SetSource(IRuneObject? source);
    }
}