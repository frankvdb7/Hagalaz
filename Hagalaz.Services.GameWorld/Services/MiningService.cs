using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class MiningService : IMiningService
    {
        private readonly IMiningOreRepository _miningOreRepository;
        private readonly IMiningPickaxeRepository _miningPickaxeRepository;
        private readonly IMiningRockRepository _miningRockRepository;
        private readonly IGameObjectService _gameObjectService;
        private readonly ILootService _lootService;
        private readonly IMapper _mapper;

        public MiningService(
            IMiningOreRepository miningOreRepository, IMiningPickaxeRepository miningPickaxeRepository, IMiningRockRepository miningRockRepository,
            IGameObjectService gameObjectService, ILootService lootService, IMapper mapper)
        {
            _miningOreRepository = miningOreRepository;
            _miningPickaxeRepository = miningPickaxeRepository;
            _miningRockRepository = miningRockRepository;
            _gameObjectService = gameObjectService;
            _lootService = lootService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<OreDto>> FindAllOres() => await _mapper.ProjectTo<OreDto>(_miningOreRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<OreDto?> FindOreByRockId(int rockId) =>
            await _mapper.ProjectTo<OreDto>(
                    _miningOreRepository.FindAll().Where(o => o.SkillsMiningRockDefinitions.Any(r => r.RockId == rockId)).AsNoTracking())
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyList<PickaxeDto>> FindAllPickaxes() =>
            await _mapper.ProjectTo<PickaxeDto>(_miningPickaxeRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<IReadOnlyList<RockDto>> FindAllRocks() =>
            await _mapper.ProjectTo<RockDto>(_miningRockRepository.FindAll().AsNoTracking()).ToListAsync();

        public async Task<ILootTable?> FindRockLootById(int rockId)
        {
            var definition = await _gameObjectService.FindGameObjectDefinitionById(rockId);
            return await _lootService.FindGameObjectLootTable(definition.LootTableId);
        }

        public Task<RockDto?> FindRockById(int rockId) =>
            _mapper.ProjectTo<RockDto>(_miningRockRepository.FindAll().Where(r => r.RockId == rockId).AsNoTracking()).FirstOrDefaultAsync();
    }
}