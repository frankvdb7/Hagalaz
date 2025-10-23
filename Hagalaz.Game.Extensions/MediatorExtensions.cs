using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="IGameMediator"/> interface.
    /// </summary>
    public static class MediatorExtensions
    {
        /// <summary>
        /// Sends a request and awaits a response, using the request's own type as the request type parameter.
        /// This is a convenience method that simplifies the call to the underlying generic GetResponseAsync method.
        /// </summary>
        /// <typeparam name="TResponse">The type of the expected response.</typeparam>
        /// <param name="mediator">The game mediator instance that will handle the request.</param>
        /// <param name="request">The request object to be sent.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        public static ValueTask<TResponse> GetResponseAsync<TResponse>(this IGameMediator mediator, IGameRequest<TResponse> request)
            where TResponse : class => mediator.GetResponseAsync<IGameRequest<TResponse>, TResponse>(request);
    }
}
