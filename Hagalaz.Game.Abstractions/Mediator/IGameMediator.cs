using System;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines the contract for the game mediator, which acts as a central message bus for in-process
    /// request/response, publish/subscribe, and message sending.
    /// </summary>
    public interface IGameMediator
    {
        /// <summary>
        /// Sends a request and awaits a response.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request message.</typeparam>
        /// <typeparam name="TResponse">The expected type of the response message.</typeparam>
        /// <param name="request">The request message object.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the response message.</returns>
        public ValueTask<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest request) 
            where TRequest : class                                                                                    
            where TResponse : class;

        /// <summary>
        /// Publishes a message to all subscribed consumers.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to be published.</param>
        public void Publish<TMessage>(TMessage message) where TMessage : class;

        /// <summary>
        /// Sends a message to a single consumer. If no consumer is configured for the message type, an exception will be thrown.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        /// <returns>A <see cref="Task"/> that is completed once the send is acknowledged.</returns>
        public Task SendAsync<TMessage>(TMessage message) where TMessage : class;

        /// <summary>
        /// Connects a consumer instance to the mediator to handle a specific message type.
        /// </summary>
        /// <typeparam name="TConsumer">The type of the consumer.</typeparam>
        /// <typeparam name="TMessage">The type of the message to be consumed.</typeparam>
        /// <param name="consumer">The consumer instance.</param>
        /// <returns>A <see cref="IGameConnectHandle"/> that can be used to disconnect the consumer.</returns>
        public IGameConnectHandle ConnectConsumer<TConsumer, TMessage>(TConsumer consumer)
            where TMessage : class
            where TConsumer : class, IGameConsumer<TMessage>;

        /// <summary>
        /// Connects a message handler delegate to the mediator for handling a specific type of message.
        /// </summary>
        /// <typeparam name="TMessage">The message type to handle.</typeparam>
        /// <param name="handler">The callback delegate to invoke when a message of the specified type is received.</param>
        /// <returns>A <see cref="IGameConnectHandle"/> that can be used to disconnect the handler.</returns>
        public IGameConnectHandle ConnectHandler<TMessage>(Func<IGameConsumeContext<TMessage>, Task> handler)
            where TMessage : class;
    }
}
