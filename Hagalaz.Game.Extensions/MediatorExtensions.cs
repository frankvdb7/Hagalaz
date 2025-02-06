using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;

namespace Hagalaz.Game.Extensions
{
    public static class MediatorExtensions
    {
        public static ValueTask<TResponse> GetResponseAsync<TResponse>(this IGameMediator mediator, IGameRequest<TResponse> request)
            where TResponse : class => mediator.GetResponseAsync<IGameRequest<TResponse>, TResponse>(request);
    }
}
