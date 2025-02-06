namespace Hagalaz.Game.Abstractions.Model.Events
{
    /// <summary>
    /// Get's called when given event is happened.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event.</typeparam>
    /// <param name="e">Event which is happened.</param>
    /// <returns>
    /// If return is true this event won't be sended to other handlers.
    /// </returns>
    public delegate bool EventHappened<TEventType>(TEventType e) where TEventType : IEvent;
    /// <summary>
    /// Get's called when given event is happened.
    /// </summary>
    /// <param name="e">Event which is happened.</param>
    /// <returns>If return is true this event won't be sended to other handlers.</returns>
    public delegate bool EventHappened(IEvent e);
}
