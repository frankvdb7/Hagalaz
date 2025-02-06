using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    public interface IGameConsumeContext<out TMessage>
        where TMessage : class
    {
        Task RespondAsync<TResponse>(TResponse response)
            where TResponse : class;
        public TMessage Message { get; }
    }
}
