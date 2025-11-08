namespace Hagalaz.Game.Abstractions.Model.Events
{
    /// <summary>
    /// Defines a contract for an event publisher.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Sends an event to its handlers.
        /// </summary>
        /// <param name="e">The event to send.</param>
        /// <returns>Returns true if the event was not handled; otherwise, false.</returns>
        bool SendEvent(IEvent e);
    }
}
