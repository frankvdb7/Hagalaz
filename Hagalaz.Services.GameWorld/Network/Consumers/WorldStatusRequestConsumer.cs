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

namespace Hagalaz.Services.GameWorld.Network.Consumers
{
    public class WorldStatusRequestConsumer : IConsumer<WorldStatusRequest>
    {
        private readonly IOptions<WorldOptions> _worldOptions;
        private readonly ICharacterService _characterService;
        private readonly IWorldRepository _worldRepository;
        private readonly IMapper _mapper;

        public WorldStatusRequestConsumer(IOptions<WorldOptions> worldOptions, ICharacterService characterService, IWorldRepository worldRepository, IMapper mapper)
        {
            _worldOptions = worldOptions;
            _characterService = characterService;
            _worldRepository = worldRepository;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<WorldStatusRequest> context)
        {
            var options = _worldOptions.Value;
            var world = await _worldRepository.FindWorldById(options.Id).FirstOrDefaultAsync() ?? throw new NotFoundException();
            var characterCount = await _characterService.CountAsync();
            var settings = _mapper.Map<WorldOnlineMessage.WorldSettings>(world);
            var location = _mapper.Map<WorldOnlineMessage.WorldLocation>(world);
            await context.RespondAsync(new WorldOnlineMessage
            {
                Id = options.Id,
                Name = options.Name,
                CharacterCount = characterCount,
                Settings = settings,
                Location = location,
                IpAddress = context.DestinationAddress?.Host ?? string.Empty
            });
        }
    }
}
