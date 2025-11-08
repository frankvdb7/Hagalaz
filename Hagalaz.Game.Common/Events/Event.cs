using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// A base class for game events.
    /// </summary>
    public abstract class Event : IEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        protected Event()
        {
        }
    }
}
