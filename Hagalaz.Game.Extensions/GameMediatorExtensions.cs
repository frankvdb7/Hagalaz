using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;

namespace Hagalaz.Game.Extensions
{
    public static class GameMediatorExtensions
    {
        /// <summary>
        /// Connects a message handler to the mediator for handling a specific type of message
        /// </summary>
        /// <typeparam name="TMessage">The message type to handle, often inferred from the callback specified</typeparam>
        /// <param name="mediator">The mediator to connect to</param>
        /// <param name="handler">
        /// The callback to invoke when messages of the specified type arrive at the service bus
        /// </param>
        public static IGameConnectHandle ConnectHandler<TMessage>(this IGameMediator mediator, Action<IGameConsumeContext<TMessage>> handler)
            where TMessage : class =>
            mediator.ConnectHandler<TMessage>(context =>
            {
                handler(context);
                return Task.CompletedTask;
            });
    }
}