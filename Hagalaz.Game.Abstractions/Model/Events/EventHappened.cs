namespace Hagalaz.Game.Abstractions.Model.Events
{
    /// <summary>
    /// Represents a delegate for handling a specific type of game event.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event being handled.</typeparam>
    /// <param name="e">The event instance that occurred.</param>
    /// <returns><c>true</c> to indicate that the event has been consumed and should not be propagated to other handlers; otherwise, <c>false</c>.</returns>
    public delegate bool EventHappened<TEventType>(TEventType e) where TEventType : IEvent;

    /// <summary>
    /// Represents a delegate for handling any type of game event.
    /// </summary>
    /// <param name="e">The event instance that occurred.</param>
    /// <returns><c>true</c> to indicate that the event has been consumed and should not be propagated to other handlers; otherwise, <c>false</c>.</returns>
    public delegate bool EventHappened(IEvent e);
}