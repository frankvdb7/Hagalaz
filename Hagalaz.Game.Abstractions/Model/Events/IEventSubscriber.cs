using System;

namespace Hagalaz.Game.Abstractions.Model.Events
{
    /// <summary>
    /// Defines a contract for an event subscriber.
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// Subscribes a handler to an event of a specific type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to subscribe to.</typeparam>
        /// <param name="handler">The handler to be called when the event occurs.</param>
        /// <returns>An <see cref="EventHappened"/> delegate that can be used to unsubscribe.</returns>
        EventHappened Listen<TEventType>(EventHappened<TEventType> handler) where TEventType : IEvent;

        /// <summary>
        /// Unsubscribes all handlers for a specific event type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to unsubscribe from.</typeparam>
        void StopListen<TEventType>() where TEventType : IEvent;

        /// <summary>
        /// Unsubscribes a specific handler for a given event type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The handler to be removed.</param>
        /// <returns>True if the handler was successfully removed; otherwise, false.</returns>
        bool StopListen<TEventType>(EventHappened handler) where TEventType : IEvent;

        /// <summary>
        /// Unsubscribes a specific handler for a given event type.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="handler">The handler to be removed.</param>
        /// <returns>True if the handler was successfully removed; otherwise, false.</returns>
        bool StopListen(Type eventType, EventHappened handler);
    }
}
