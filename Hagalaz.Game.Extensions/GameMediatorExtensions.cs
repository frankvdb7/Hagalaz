using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IGameMediator"/> interface.
    /// </summary>
    public static class GameMediatorExtensions
    {
        /// <summary>
        /// Connects a synchronous message handler to the mediator for handling a specific type of message.
        /// This is a convenience overload for handlers that do not need to perform asynchronous operations.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to handle.</typeparam>
        /// <param name="mediator">The game mediator instance to connect the handler to.</param>
        /// <param name="handler">The synchronous callback to invoke when a message of the specified type is received.</param>
        /// <returns>A handle that can be used to disconnect the handler.</returns>
        public static IGameConnectHandle ConnectHandler<TMessage>(this IGameMediator mediator, Action<IGameConsumeContext<TMessage>> handler)
            where TMessage : class =>
            mediator.ConnectHandler<TMessage>(context =>
            {
                handler(context);
                return Task.CompletedTask;
            });
    }
}