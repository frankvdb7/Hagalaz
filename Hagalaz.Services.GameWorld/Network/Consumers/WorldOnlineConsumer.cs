using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class WorldOnlineConsumer : IConsumer<WorldOnlineMessage>
    {
        private readonly IWorldInfoService _worldInfoService;
        private readonly IMapper _mapper;

        public WorldOnlineConsumer(IWorldInfoService worldInfoService, IMapper mapper)
        {
            _worldInfoService = worldInfoService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<WorldOnlineMessage> context)
        {
            var message = context.Message;
            var info = _mapper.Map<WorldInfo>(message);
            await _worldInfoService.AddOrUpdateWorldInfoAsync(info with
            {
                IpAddress = context.SourceAddress?.Host ?? string.Empty,
            });

            await _worldInfoService.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(message.Id, message.CharacterCount, true));
        }
    }
}