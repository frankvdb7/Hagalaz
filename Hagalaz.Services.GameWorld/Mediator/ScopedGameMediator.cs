using Hagalaz.Game.Abstractions.Mediator;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Mediator
{
    public class ScopedGameMediator : GameMediator, IScopedGameMediator
    {
        public ScopedGameMediator(IScopedMediator mMediator, ILogger<ScopedGameMediator> logger) : base(mMediator, logger)
        {
        }
    }
}
