using System;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hagalaz.Exceptions;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    [ExcludeFromConfigureEndpoints]
    public class WorldStatusRequestConsumer : IConsumer<WorldStatusRequest>
    {
        private readonly IOptions<WorldOptions> _worldOptions;
        private readonly ICharacterService _characterService;
        private readonly IWorldRepository _worldRepository;
        private readonly IMapper _mapper;
        private readonly WorldInstanceIdentity _identity;
        private readonly IPublishEndpoint _publishEndpoint;

        public WorldStatusRequestConsumer(
            IOptions<WorldOptions> worldOptions,
            ICharacterService characterService,
            IWorldRepository worldRepository,
            IMapper mapper,
            WorldInstanceIdentity identity,
            IPublishEndpoint publishEndpoint)
        {
            _worldOptions = worldOptions;
            _characterService = characterService;
            _worldRepository = worldRepository;
            _mapper = mapper;
            _identity = identity;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<WorldStatusRequest> context)
        {
            var options = _worldOptions.Value;
            var world = await _worldRepository.FindWorldById(options.Id).FirstOrDefaultAsync(context.CancellationToken)
                ?? throw new NotFoundException();
            var characterCount = await _characterService.CountAsync();
            var settings = _mapper.Map<WorldOnlineMessage.WorldSettings>(world);
            var location = _mapper.Map<WorldOnlineMessage.WorldLocation>(world);
            var now = DateTimeOffset.UtcNow;
            var onlineMessage = new WorldOnlineMessage
            {
                Id = options.Id,
                Name = options.Name,
                CharacterCount = characterCount,
                Settings = settings,
                Location = location,
                IpAddress = options.AdvertisedEndpoint.Host,
                Port = options.AdvertisedEndpoint.Port,
                InstanceId = _identity.InstanceId,
                Generation = _identity.Generation,
                StartedAt = _identity.StartedAt,
                LastSeenAt = now,
                LeaseExpiresAt = now + options.RegistrationLeaseDuration
            };

            if (context.ResponseAddress != null)
            {
                await context.RespondAsync(onlineMessage);
            }
            await _publishEndpoint.Publish(onlineMessage, context.CancellationToken);
        }
    }
}
