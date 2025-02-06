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
    public class FiremakingService : IFiremakingService
    {
        private readonly IFiremakingRepository _firemakingRepository;
        private readonly IMapper _mapper;

        public FiremakingService(IFiremakingRepository firemakingRepository, IMapper mapper)
        {
            _firemakingRepository = firemakingRepository;
            _mapper = mapper;
        }

        public Task<FiremakingDto?> FindByLogId(int logId) => _mapper.ProjectTo<FiremakingDto>(_firemakingRepository.FindAll().Where(f => f.ItemId == logId)).FirstOrDefaultAsync();

        public async Task<IReadOnlyList<FiremakingDto>> FindAllLogs() => await _mapper.ProjectTo<FiremakingDto>(_firemakingRepository.FindAll()).ToListAsync();
    }
}