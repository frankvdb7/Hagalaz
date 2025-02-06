using System.Threading.Tasks;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class WorldOfflineConsumer : IConsumer<WorldOfflineMessage>
    {
        private readonly IWorldInfoService _worldInfoService;

        public WorldOfflineConsumer(IWorldInfoService worldInfoService)
        {
            _worldInfoService = worldInfoService;
        }

        public async Task Consume(ConsumeContext<WorldOfflineMessage> context)
        {
            var message = context.Message;
            await _worldInfoService.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(message.Id, 0, false));
        }
    }
}