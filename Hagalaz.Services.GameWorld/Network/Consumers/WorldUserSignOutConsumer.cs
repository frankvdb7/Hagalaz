using System.Threading.Tasks;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class WorldUserSignOutConsumer : IConsumer<WorldUserSignOutMessage>
    {
        private readonly IWorldInfoService _worldInfoService;

        public WorldUserSignOutConsumer(IWorldInfoService worldInfoService)
        {
            _worldInfoService = worldInfoService;
        }

        public async Task Consume(ConsumeContext<WorldUserSignOutMessage> context)
        {
            var message = context.Message;
            await _worldInfoService.RemoveCharacter(new WorldCharacter
            (
                message.MasterId,
                message.WorldId
            ));
        }
    }
}
