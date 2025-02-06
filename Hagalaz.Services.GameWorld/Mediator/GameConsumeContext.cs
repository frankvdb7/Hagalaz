using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public sealed class GameConsumeContext<TMessage> : IGameConsumeContext<TMessage>
        where TMessage : class
    {
        private readonly ConsumeContext<TMessage> _context;

        public TMessage Message => _context.Message;

        public GameConsumeContext(ConsumeContext<TMessage> context) => _context = context;

        public Task RespondAsync<TResponse>(TResponse response) where TResponse : class => _context.RespondAsync(response);
    }
}
