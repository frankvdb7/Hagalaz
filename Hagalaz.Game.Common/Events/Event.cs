using System;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Class for various events.
    /// </summary>
    public abstract class Event : IEvent
    {
        /// <summary>
        /// Contains boolean if this event was already sended
        /// to the world.
        /// </summary>
        /// <value><c>true</c> if happened; otherwise, <c>false</c>.</value>
        private bool Happened { get; set; }

        /// <summary>
        /// Constructs new event.
        /// </summary>
        protected Event() => Happened = false;

        /// <summary>
        /// Send's this to world, throws exception if this event is
        /// already been sended.
        /// </summary>
        /// <returns>If event was NOT caught.</returns>
        /// <exception cref="System.Exception"></exception>
        public bool Send()
        {
            if (Happened)
                throw new Exception("Event was already sent!");
            Happened = true;
            var eventManager = ServiceLocator.Current.GetInstance<IEventManager>();
            return eventManager.SendEvent(this);
        }
    }
}