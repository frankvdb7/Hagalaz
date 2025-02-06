namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDialogueScript : IWidgetScript
    {
        /// <summary>
        /// Sets the source that the character interacted with.
        /// </summary>
        void SetSource(IRuneObject? source);
    }
}
