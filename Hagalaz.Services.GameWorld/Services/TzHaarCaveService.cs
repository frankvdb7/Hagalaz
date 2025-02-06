using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class TzHaarCaveService : ITzHaarCaveService
    {
        private readonly ITzhaarWaveDefinitionRepository _waveDefinitionRepository;
        private readonly IMapper _mapper;

        public TzHaarCaveService(ITzhaarWaveDefinitionRepository waveDefinitionRepository, IMapper mapper)
        {
            _waveDefinitionRepository = waveDefinitionRepository;
            _mapper = mapper;
        }


        public async Task<IReadOnlyList<WaveDto>> FindAllWaves() => await _mapper.ProjectTo<WaveDto>(_waveDefinitionRepository.FindAll()).ToListAsync();
    }
}