using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class FarmingService : IFarmingService
    {
        private readonly IFarmingPatchDefinitionRepository _farmingPatchDefinitionRepository;
        private readonly IFarmingSeedDefinitionRepository _seedDefinitionRepository;
        private readonly IMapper _mapper;

        public FarmingService(IFarmingPatchDefinitionRepository farmingPatchDefinitionRepository, IFarmingSeedDefinitionRepository seedDefinitionRepository, IMapper mapper)
        {
            _farmingPatchDefinitionRepository = farmingPatchDefinitionRepository;
            _seedDefinitionRepository = seedDefinitionRepository;
            _mapper = mapper;
        }


        public async Task<IReadOnlyList<PatchDto>> FindAllPatches() => await _mapper.ProjectTo<PatchDto>(_farmingPatchDefinitionRepository.FindAll()).ToListAsync();

        public Task<SeedDto?> FindSeedById(int itemId) => _mapper.ProjectTo<SeedDto>(_seedDefinitionRepository.FindAll().Where(s => s.ItemId == itemId)).FirstOrDefaultAsync();

        public Task<PatchDto?> FindPatchById(int objectId) => _mapper.ProjectTo<PatchDto>(_farmingPatchDefinitionRepository.FindAll().Where(p => p.ObjectId == objectId)).FirstOrDefaultAsync();
    }
}