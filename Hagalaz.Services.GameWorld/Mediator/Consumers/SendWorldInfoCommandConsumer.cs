using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Services;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class SendWorldInfoCommandConsumer : IConsumer<SendWorldInfoCommand>
    {
        private readonly IWorldInfoService _worldInfoService;
        private readonly IMapper _mapper;

        public SendWorldInfoCommandConsumer(IWorldInfoService worldInfoService, IMapper mapper)
        {
            _worldInfoService = worldInfoService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<SendWorldInfoCommand> context)
        {
            var message = context.Message;
            var cacheTask = _worldInfoService.GetCacheAsync();
            var characterInfoTask = _worldInfoService.FindAllWorldCharacterInfoAsync();

            var cache = await cacheTask;
            var characterInfo = await characterInfoTask;
            var setWorldInfoMessage = new SetWorldInfoMessage
            {
                FullUpdate = cache.Checksum != message.Checksum,
                LocationInfos = _mapper.Map<IReadOnlyList<SetWorldInfoMessage.WorldLocationInfoDto>>(cache.LocationInfos),
                WorldInfos = _mapper.Map<IReadOnlyList<SetWorldInfoMessage.WorldInfoDto>>(cache.WorldInfos),
                CharacterInfos = _mapper.Map<IReadOnlyList<SetWorldInfoMessage.WorldCharacterInfoDto>>(characterInfo),
                Checksum = cache.Checksum
            };

            message.Session.SendMessage(setWorldInfoMessage);
        }
    }
}
