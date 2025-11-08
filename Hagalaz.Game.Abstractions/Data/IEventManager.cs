using System;
using Hagalaz.Game.Abstractions.Model.Events;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// Defines the contract for an event manager, which handles the registration, unregistration, and dispatching of game events.
    /// </summary>
    public interface IEventManager : IEventBus
    {
    }
}
