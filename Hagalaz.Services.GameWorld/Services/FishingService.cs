using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class FishingService
    {
        private readonly IFishingSpotDefinitionRepository _fishingSpotDefinitionRepository;
        private readonly IMapper _mapper;

        public FishingService(IFishingSpotDefinitionRepository fishingSpotDefinitionRepository, IMapper mapper)
        {
            _fishingSpotDefinitionRepository = fishingSpotDefinitionRepository;
            _mapper = mapper;
        }

        public async Task<FishingSpotTable?> FindSpotByNpcId(int npcId, CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<FishingSpotTable>(_fishingSpotDefinitionRepository.FindAll()
                    .Where(f => f.SkillsFishingSpotNpcDefinitions.Any(s => s.NpcId == npcId)))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<FishingSpotTable?> FindSpotByNpcIdClickType(int npcId, NpcClickType clickType, CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<FishingSpotTable>(_fishingSpotDefinitionRepository.FindAll()
                    .Where(f => f.SkillsFishingSpotNpcDefinitions.Any(s => s.NpcId == npcId) && f.ClickType == clickType.ToString()))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<FishingSpotTable>> FindAllSpots(CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<FishingSpotTable>(_fishingSpotDefinitionRepository.FindAll()).ToListAsync(cancellationToken);
    }
}