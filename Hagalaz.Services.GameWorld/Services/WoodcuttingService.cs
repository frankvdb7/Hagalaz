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
    /// <summary>
    /// 
    /// </summary>
    public class WoodcuttingService : IWoodcuttingService
    {
        private readonly IWoodcuttingHatchetDefinitionRepository _hatchetDefinitionRepository;
        private readonly IWoodcuttingTreeDefinitionRepository _treeDefinitionRepository;
        private readonly IWoodcuttingLogDefinitionRepository _logDefinitionRepository;
        private readonly IMapper _mapper;

        public WoodcuttingService(IWoodcuttingHatchetDefinitionRepository hatchetDefinitionRepository, IWoodcuttingTreeDefinitionRepository treeDefinitionRepository, IWoodcuttingLogDefinitionRepository logDefinitionRepository, IMapper mapper)
        {
            _hatchetDefinitionRepository = hatchetDefinitionRepository;
            _treeDefinitionRepository = treeDefinitionRepository;
            _logDefinitionRepository = logDefinitionRepository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<LogDto>> FindAllLogs() => await _mapper.ProjectTo<LogDto>(_logDefinitionRepository.FindAll()).ToListAsync();
        public async Task<LogDto?> FindLogByTreeId(int treeId) => await _mapper.ProjectTo<LogDto>(_treeDefinitionRepository.FindAll().Where(i => i.TreeId == treeId)).FirstOrDefaultAsync();
        public async Task<IReadOnlyList<HatchetDto>> FindAllHatchets() => await _mapper.ProjectTo<HatchetDto>(_hatchetDefinitionRepository.FindAll()).ToListAsync();
        public async Task<IReadOnlyList<TreeDto>> FindAllTrees() => await _mapper.ProjectTo<TreeDto>(_treeDefinitionRepository.FindAll()).ToListAsync();
        public async Task<TreeDto?> FindTreeById(int id) => await _mapper.ProjectTo<TreeDto>(_treeDefinitionRepository.FindAll().Where(i => i.TreeId == id)).FirstOrDefaultAsync();
    }
}