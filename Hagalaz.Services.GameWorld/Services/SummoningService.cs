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
    public class SummoningService : ISummoningService
    {
        private readonly ISummoningDefinitionRepository _summoningDefinitionRepository;
        private readonly IMapper _mapper;

        public SummoningService(ISummoningDefinitionRepository summoningDefinitionRepository, IMapper mapper)
        {
            _summoningDefinitionRepository = summoningDefinitionRepository;
            _mapper = mapper;
        }

        public async Task<SummoningDto?> FindDefinitionByNpcId(int npcId) => await _mapper.ProjectTo<SummoningDto>(_summoningDefinitionRepository.FindAll().Where(e => e.NpcId == npcId)).FirstOrDefaultAsync();

        public async Task<SummoningDto?> FindDefinitionByPouchId(int pouchId) => await _mapper.ProjectTo<SummoningDto>(_summoningDefinitionRepository.FindAll().Where(e => e.PouchId == pouchId)).FirstOrDefaultAsync();

        public async Task<SummoningDto?> FindDefinitionByScrollId(int scrollId) => await _mapper.ProjectTo<SummoningDto>(_summoningDefinitionRepository.FindAll().Where(e => e.ScrollId == scrollId)).FirstOrDefaultAsync();

        public async Task<IReadOnlyList<SummoningDto>> FindAllDefinitions() => await _mapper.ProjectTo<SummoningDto>(_summoningDefinitionRepository.FindAll()).ToListAsync();
    }
}