using System;
using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEventManager
    {
        /// <summary>
        /// Listens the specified handler.
        /// </summary>
        /// <typeparam name="TEventType">The type of the vent type.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        EventHappened Listen<TEventType>(EventHappened<TEventType> handler) where TEventType : IEvent;
        /// <summary>
        /// Stops the listen.
        /// </summary>
        /// <typeparam name="TEventType">The type of the vent type.</typeparam>
        void StopListen<TEventType>() where TEventType : IEvent;
        /// <summary>
        /// Stops the listen.
        /// </summary>
        /// <typeparam name="TEventType">The type of the vent type.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        bool StopListen<TEventType>(EventHappened handler) where TEventType : IEvent;
        /// <summary>
        /// Stops the listen.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        bool StopListen(Type eventType, EventHappened handler);
        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        bool SendEvent(IEvent e);
    }
}
