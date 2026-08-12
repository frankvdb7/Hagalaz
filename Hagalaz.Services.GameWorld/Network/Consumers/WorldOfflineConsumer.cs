using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    [ExcludeFromConfigureEndpoints]
    public class WorldOfflineConsumer : IConsumer<WorldOfflineMessage>
    {
        private readonly IWorldInfoService _worldInfoService;
        private readonly WorldRegistrationStore _registrations;
        private readonly IMapper _mapper;

        public WorldOfflineConsumer(
            IWorldInfoService worldInfoService,
            WorldRegistrationStore registrations,
            IMapper mapper)
        {
            _worldInfoService = worldInfoService;
            _registrations = registrations;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<WorldOfflineMessage> context)
        {
            var update = _registrations.ObserveOffline(context.Message);
            if (update.ActiveMessage == null)
            {
                await _worldInfoService.UpdateWorldCharacterInfoAsync(new WorldCharacterInfo(update.WorldId, 0, false));
                return;
            }

            await _worldInfoService.AddOrUpdateWorldInfoAsync(_mapper.Map<WorldInfo>(update.ActiveMessage));
            await _worldInfoService.UpdateWorldCharacterInfoAsync(
                new WorldCharacterInfo(update.WorldId, update.ActiveMessage.CharacterCount, true));
        }
    }
}
