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
    public class RunecraftingService : IRunecraftingService
    {
        private readonly IRunecraftingRepository _runecraftingRepository;
        private readonly IMapper _mapper;

        public RunecraftingService(IRunecraftingRepository runecraftingRepository, IMapper mapper)
        {
            _runecraftingRepository = runecraftingRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<RunecraftingDto>> FindAllAltars() => await _mapper.ProjectTo<RunecraftingDto>(_runecraftingRepository.FindAll()).ToListAsync();

        public Task<RunecraftingDto?> FindAltarById(int altarId) => _mapper.ProjectTo<RunecraftingDto>(_runecraftingRepository.FindAll().Where(e => e.AltarId == altarId)).FirstOrDefaultAsync();

        public Task<RunecraftingDto?> FindAltarByRuinId(int ruinId) => _mapper.ProjectTo<RunecraftingDto>(_runecraftingRepository.FindAll().Where(e => e.RuinId == ruinId)).FirstOrDefaultAsync();

        public Task<RunecraftingDto?> FindAltarByRiftId(int riftId) => _mapper.ProjectTo<RunecraftingDto>(_runecraftingRepository.FindAll().Where(e => e.RiftId == riftId)).FirstOrDefaultAsync();

        public Task<RunecraftingDto?> FindAltarByPortalId(int portalId) => _mapper.ProjectTo<RunecraftingDto>(_runecraftingRepository.FindAll().Where(e => e.PortalId == portalId)).FirstOrDefaultAsync();
    }
}