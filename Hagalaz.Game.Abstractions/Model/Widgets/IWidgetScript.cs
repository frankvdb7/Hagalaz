namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWidgetScript
    {
        /// <summary>
        /// Get's called when interface script instance is created.
        /// </summary>
        /// <param name="inter">Interface instance of opened interface.</param>
        void Initialize(IWidget inter);
        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        void OnOpen();
        /// <summary>
        /// Happens when interface has been opened for character.
        /// </summary>
        void OnOpened();
        /// <summary>
        /// Happens when interface is closed for character.
        /// </summary>
        void OnClose();
        /// <summary>
        /// Happens when interface has been closed for character.
        /// </summary>
        void OnClosed();
        /// <summary>
        /// Called when the display and the frame has been changed for the character.
        /// Calling this will re-register the interface on the new frame.
        /// </summary>
        void OnDisplayChanged();
        /// <summary>
        /// Determines whether this instance can be interrupted.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can be interrupted; otherwise, <c>false</c>.
        /// </returns>
        bool CanInterrupt();
    }
}
