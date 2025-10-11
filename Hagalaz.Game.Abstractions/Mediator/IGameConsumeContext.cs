using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines the context for a consumer of a game message, providing access to the message itself and a way to respond.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being consumed.</typeparam>
    public interface IGameConsumeContext<out TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Sends a response back to the original requester.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response message.</typeparam>
        /// <param name="response">The response message object.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous response operation.</returns>
        Task RespondAsync<TResponse>(TResponse response)
            where TResponse : class;

        /// <summary>
        /// Gets the message that is being consumed.
        /// </summary>
        public TMessage Message { get; }
    }
}
