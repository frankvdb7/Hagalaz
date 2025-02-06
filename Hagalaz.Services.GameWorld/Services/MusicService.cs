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

    public class MusicService : IMusicService
    {
        private readonly IMusicLocationRepository _musicLocationRepository;
        private readonly IMusicDefinitionRepository _musicDefinitionRepository;
        private readonly IMapper _mapper;

        public MusicService(IMusicLocationRepository musicLocationRepository, IMusicDefinitionRepository musicDefinitionRepository, IMapper mapper)
        {
            _musicLocationRepository = musicLocationRepository;
            _musicDefinitionRepository = musicDefinitionRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<int>> FindMusicIdsByRegionId(int regionId) => await _musicLocationRepository.FindAll()
            .Where(m => m.RegionId == regionId)
            .Select(m => (int)m.MusicId)
            .ToListAsync();

        public async Task<MusicDto?> FindMusicByIndex(int index) => await _mapper.ProjectTo<MusicDto>(_musicDefinitionRepository.FindAll()
            .Where(m => m.Id == index))
            .FirstOrDefaultAsync();
    }
}