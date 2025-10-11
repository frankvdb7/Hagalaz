using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines the contract for a consumer of a specific type of game message.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message that this consumer can handle.</typeparam>
    public interface IGameConsumer<in TMessage> : IGameConsumer
        where TMessage : class
    {
        /// <summary>
        /// Consumes the specified message.
        /// </summary>
        /// <param name="context">The consumption context, which contains the message and provides a way to respond.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous consumption operation.</returns>
        Task Consume(IGameConsumeContext<TMessage> context);
    }

    /// <summary>
    /// A non-generic marker interface for game consumers, used for discovery and registration in the dependency injection container.
    /// </summary>
    public interface IGameConsumer { }
}
