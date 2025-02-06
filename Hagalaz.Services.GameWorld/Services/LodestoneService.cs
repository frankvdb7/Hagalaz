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
    public class LodestoneService : ILodestoneService
    {
        private readonly ILodestoneRepository _lodestoneRepository;
        private readonly IMapper _mapper;

        public LodestoneService(ILodestoneRepository lodestoneRepository, IMapper mapper)
        {
            _lodestoneRepository = lodestoneRepository;
            _mapper = mapper;
        }

        public Task<LodestoneDto?> FindByGameObjectId(int id) => _mapper.ProjectTo<LodestoneDto>(_lodestoneRepository.FindAll().Where(l => l.GameobjectId == id).AsNoTracking()).FirstOrDefaultAsync();

        public async Task<IReadOnlyList<LodestoneDto>> FindAll() => await _mapper.ProjectTo<LodestoneDto>(_lodestoneRepository.FindAll().AsNoTracking()).ToListAsync();
    }
}