using Hagalaz.Game.Abstractions.Mediator;
using MassTransit.Mediator;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public class ScopedGameMediator : GameMediator, IScopedGameMediator
    {
        public ScopedGameMediator(IScopedMediator mMediator) : base(mMediator)
        {
        }
    }
}
