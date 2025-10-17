using System;
using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// Defines the contract for an event manager, which handles the registration, unregistration, and dispatching of game events.
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        /// Registers an event handler for a specific type of event.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to listen for.</typeparam>
        /// <param name="handler">The delegate to be executed when the event occurs.</param>
        /// <returns>The registered event handler instance, which can be used for unregistering.</returns>
        EventHappened Listen<TEventType>(EventHappened<TEventType> handler) where TEventType : IEvent;

        /// <summary>
        /// Unregisters all event handlers for a specific type of event.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to stop listening for.</typeparam>
        void StopListen<TEventType>() where TEventType : IEvent;

        /// <summary>
        /// Unregisters a specific event handler for a given event type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">The handler instance to unregister.</param>
        /// <returns><c>true</c> if the handler was successfully unregistered; otherwise, <c>false</c>.</returns>
        bool StopListen<TEventType>(EventHappened handler) where TEventType : IEvent;

        /// <summary>
        /// Unregisters a specific event handler for a given event type.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="handler">The handler instance to unregister.</param>
        /// <returns><c>true</c> if the handler was successfully unregistered; otherwise, <c>false</c>.</returns>
        bool StopListen(Type eventType, EventHappened handler);

        /// <summary>
        /// Dispatches an event to all registered handlers for that event type.
        /// </summary>
        /// <param name="e">The event to send.</param>
        /// <returns><c>true</c> if the event was consumed by a handler and propagation was stopped; otherwise, <c>false</c>.</returns>
        bool SendEvent(IEvent e);
    }
}