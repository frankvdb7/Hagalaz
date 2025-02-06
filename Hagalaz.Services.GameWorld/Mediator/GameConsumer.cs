using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public sealed class GameConsumer<TMessage> : IConsumer<TMessage> where TMessage : class
    {
        private readonly IGameConsumer<TMessage> _consumer;

        public GameConsumer(IGameConsumer<TMessage> consumer) => _consumer = consumer;

        public Task Consume(ConsumeContext<TMessage> context) => _consumer.Consume(new GameConsumeContext<TMessage>(context));
    }
}
