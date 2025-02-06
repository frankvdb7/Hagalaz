using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// Class for managing events.
    /// </summary>
    public class EventManager : IEventManager
    {
        /// <summary>
        /// Contains binded event handlers.
        /// </summary>
        private readonly Dictionary<Type, List<EventHappened>> _eventHandlers = new Dictionary<Type, List<EventHappened>>();
        /// <summary>
        /// Contains lock object for this class.
        /// </summary>
        private readonly object _lockObject = new object();
        /// <summary>
        /// Contains count of unhandled events.
        /// </summary>
        /// <value>The unhandled events.</value>
        public int UnhandledEvents { get; private set; }

        /// <summary>
        /// Send's event to it's handlers.
        /// </summary>
        /// <param name="e">Event which should be sended.</param>
        /// <returns>Returns true if event was not caught.</returns>
        public bool SendEvent(IEvent e)
        {
            List<EventHappened> eventHandlers;
            lock (_lockObject)
            {
                if (!_eventHandlers.ContainsKey(e.GetType()))
                {
                    UnhandledEvents++;
                    return true; // unhandled event.
                }
                eventHandlers = [.._eventHandlers[e.GetType()]];
                if (eventHandlers.Count <= 0)
                {
                    UnhandledEvents++;
                    return true; // unhandled event.
                }
            }
            if (eventHandlers.Any(eventHandler => eventHandler.Invoke(e)))
            {
                return false;
            };
            return true;
        }


        /// <summary>
        /// Listens to event occurrences of given type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event.</typeparam>
        /// <param name="handler">Handler which will be called when event occurs.</param>
        /// <returns></returns>
        public EventHappened Listen<TEventType>(EventHappened<TEventType> handler) where TEventType : IEvent
        {
            lock (_lockObject)
            {
                var type = typeof(TEventType);
                bool Handle(IEvent e) => handler((TEventType)e);
                if (_eventHandlers.ContainsKey(type))
                    _eventHandlers[type].Add(Handle);
                else
                {
                    var handlerList = new List<EventHappened>
                    {
                        Handle
                    };
                    _eventHandlers.Add(type, handlerList);
                }
                return Handle;
            }
        }

        /// <summary>
        /// Stops all listeners for given type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the vent type.</typeparam>
        public void StopListen<TEventType>() where TEventType : IEvent
        {
            lock (_lockObject)
            {
                var type = typeof(TEventType);
                if (!_eventHandlers.ContainsKey(type))
                    return;
                _eventHandlers[type].Clear();
            }
        }

        /// <summary>
        /// Stops specified handler which listens for given type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the vent type.</typeparam>
        /// <param name="handler">Handler that must be removed.</param>
        public bool StopListen<TEventType>(EventHappened handler) where TEventType : IEvent
        {
            lock (_lockObject)
            {
                var type = typeof(TEventType);
                return _eventHandlers.ContainsKey(type) && _eventHandlers[type].Remove(handler);
            }
        }

        /// <summary>
        /// Stops specified handler which listens for given type.
        /// </summary>
        /// <param name="eventType">The type of the vent type.</param>
        /// <param name="handler">Handler that must be removed.</param>
        public bool StopListen(Type eventType, EventHappened handler)
        {
            lock (_lockObject)
            {
                return _eventHandlers.ContainsKey(eventType) && _eventHandlers[eventType].Remove(handler);
            }
        }
    }
}
