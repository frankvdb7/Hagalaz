using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Mediator
{
    public interface IGameConsumer<in TMessage> : IGameConsumer
        where TMessage : class
    {
        Task Consume(IGameConsumeContext<TMessage> context);
    }

    public interface IGameConsumer { }
}
