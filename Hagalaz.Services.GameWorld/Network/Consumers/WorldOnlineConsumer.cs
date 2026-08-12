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
        private readonly WorldRegistrationStore _registrations;

        public WorldOnlineConsumer(
            IWorldInfoService worldInfoService,
            IMapper mapper,
            WorldRegistrationStore registrations)
        {
            _worldInfoService = worldInfoService;
            _mapper = mapper;
            _registrations = registrations;
        }

        public async Task Consume(ConsumeContext<WorldOnlineMessage> context)
        {
            var message = context.Message;
            var update = _registrations.ObserveOnline(message);
            if (!update.IsAvailable || update.ActiveMessage == null)
            {
                await _worldInfoService.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(message.Id, 0, false));
                return;
            }

            var activeMessage = update.ActiveMessage;
            await _worldInfoService.AddOrUpdateWorldInfoAsync(_mapper.Map<WorldInfo>(activeMessage));
            await _worldInfoService.UpdateWorldCharacterInfoAsync(
                new WorldCharacterInfo(activeMessage.Id, activeMessage.CharacterCount, true));
        }
    }
}
