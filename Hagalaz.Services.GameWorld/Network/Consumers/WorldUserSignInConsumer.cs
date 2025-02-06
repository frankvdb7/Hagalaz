using System.Threading.Tasks;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class WorldUserSignInConsumer : IConsumer<WorldUserSignInMessage>
    {
        private readonly IWorldInfoService _worldInfoService;

        public WorldUserSignInConsumer(IWorldInfoService worldInfoService) => _worldInfoService = worldInfoService;

        public async Task Consume(ConsumeContext<WorldUserSignInMessage> context)
        {
            var message = context.Message;
            await _worldInfoService.AddCharacter(new WorldCharacter
            (
                message.MasterId,
                message.WorldId
            ));
        }
    }
}
