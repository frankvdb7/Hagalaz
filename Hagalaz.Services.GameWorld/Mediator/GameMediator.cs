using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public class GameMediator : IGameMediator
    {
        private readonly IMediator _mMediator;

        private readonly ILogger _logger;

        public GameMediator(IMediator mMediator, ILogger<GameMediator> logger)
        {
            _mMediator = mMediator;
            _logger = logger;
        }

        protected GameMediator(IMediator mMediator, ILogger logger)
        {
            _mMediator = mMediator;
            _logger = logger;
        }

        public void Publish<TMessage>(TMessage message) where TMessage : class =>
            _mMediator.Publish(message).ContinueWith(
                task => _logger.LogError(
                    task.Exception!.GetBaseException(),
                    "Publishing message of type '{MessageType}' failed.",
                    typeof(TMessage).Name),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class =>
            _mMediator.Send(message, cancellationToken);

        public async ValueTask<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest request) 
            where TRequest : class
            where TResponse : class
        {
            var client = _mMediator.CreateRequestClient<TRequest>();
            var response = await client.GetResponse<TResponse>(request);
            return response.Message;
        }

        public IGameConnectHandle ConnectConsumer<TConsumer, TMessage>(TConsumer consumer)
            where TMessage : class
            where TConsumer : class, IGameConsumer<TMessage>
        {
            var handle = _mMediator.ConnectConsumer(() => new GameConsumer<TMessage>(consumer));
            return new GameHandle(handle);
        }

        public IGameConnectHandle ConnectHandler<TMessage>(Func<IGameConsumeContext<TMessage>, Task> handler)
            where TMessage : class
        {
            var connect = _mMediator.ConnectHandler<TMessage>(context => handler(new GameConsumeContext<TMessage>(context)));
            return new GameHandle(connect);
        }
    }
}
