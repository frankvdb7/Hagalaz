namespace Hagalaz.Game.Abstractions.Model.Events
{
    /// <summary>
    /// Defines a contract for an event bus, which combines the functionality of an event publisher and subscriber.
    /// </summary>
    public interface IEventBus : IEventPublisher, IEventSubscriber
    {
    }
}
