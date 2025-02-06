using System;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    public interface IGameMediator
    {
        /// <summary>
        /// Create a request, and return a task for the specified response type
        /// </summary>
        /// <param name="request">The request message</param>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        public ValueTask<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest request) 
            where TRequest : class                                                                                    
            where TResponse : class;
        /// <summary>
        /// <para>
        /// Publishes a message to all subscribed consumers for the message type as specified
        /// by the generic parameter. The second parameter allows the caller to customize the
        /// outgoing publish context and set things like headers on the message.
        /// </para>
        /// </summary>
        /// <typeparam name="TMessage">The type of the message</typeparam>
        /// <param name="message">The messages to be published</param>
        public void Publish<TMessage>(TMessage message) where TMessage : class;
        /// <summary>
        /// <para>
        /// Send a message
        /// </para>
        /// <para>
        /// If there is no consumer configured for the message type, an exception will be thrown.
        /// </para>
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message</param>
        /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
        public Task SendAsync<TMessage>(TMessage message) where TMessage : class;
        /// <summary>
        /// Connect a consumer to the mediator
        /// </summary>
        /// <typeparam name="TConsumer"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public IGameConnectHandle ConnectConsumer<TConsumer, TMessage>(TConsumer consumer)
            where TMessage : class
            where TConsumer : class, IGameConsumer<TMessage>;

        /// <summary>
        /// Connects a message handler to the mediator for handling a specific type of message
        /// </summary>
        /// <typeparam name="TMessage">The message type to handle, often inferred from the callback specified</typeparam>
        /// <param name="handler">
        /// The callback to invoke when messages of the specified type arrive at the service bus
        /// </param>
        public IGameConnectHandle ConnectHandler<TMessage>(Func<IGameConsumeContext<TMessage>, Task> handler)
            where TMessage : class;
    }
}
