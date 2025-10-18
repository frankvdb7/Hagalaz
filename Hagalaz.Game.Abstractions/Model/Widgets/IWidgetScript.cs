namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Defines the contract for a script that controls a widget's behavior and lifecycle events.
    /// </summary>
    public interface IWidgetScript
    {
        /// <summary>
        /// Initializes the script with its owning widget instance.
        /// </summary>
        /// <param name="widget">The widget that this script is attached to.</param>
        void Initialize(IWidget widget);

        /// <summary>
        /// A callback executed when the widget is about to be opened.
        /// </summary>
        void OnOpen();

        /// <summary>
        /// A callback executed after the widget has been opened.
        /// </summary>
        void OnOpened();

        /// <summary>
        /// A callback executed when the widget is about to be closed.
        /// </summary>
        void OnClose();

        /// <summary>
        /// A callback executed after the widget has been closed.
        /// </summary>
        void OnClosed();

        /// <summary>
        /// A callback executed when the client's display mode changes, allowing the widget to re-register itself on the new frame if necessary.
        /// </summary>
        void OnDisplayChanged();

        /// <summary>
        /// Checks if this widget can be interrupted by other actions.
        /// </summary>
        /// <returns><c>true</c> if the widget can be interrupted; otherwise, <c>false</c>.</returns>
        bool CanInterrupt();
    }
}