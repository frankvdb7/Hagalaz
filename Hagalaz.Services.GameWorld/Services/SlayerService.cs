using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services
{
    public class SlayerService
    {
        private readonly ISlayerMasterDefinitionRepository _slayerMasterDefinitionRepository;
        private readonly ISlayerTaskDefinitionRepository _slayerTaskDefinitionRepository;
        private readonly IMapper _mapper;

        public SlayerService(
            ISlayerMasterDefinitionRepository slayerMasterDefinitionRepository,
            ISlayerTaskDefinitionRepository slayerTaskDefinitionRepository, IMapper mapper)
        {
            _slayerMasterDefinitionRepository = slayerMasterDefinitionRepository;
            _slayerTaskDefinitionRepository = slayerTaskDefinitionRepository;
            _mapper = mapper;
        }

        public async Task<SlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID, CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<SlayerTaskDefinition>(_slayerTaskDefinitionRepository.FindAll().Where(t => t.Id == taskID))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<SlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId, CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<SlayerMasterTable>(_slayerMasterDefinitionRepository.FindAll().Where(m => m.NpcId == npcId))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<SlayerMasterTable>> FindAllSlayerMasterTables(CancellationToken cancellationToken = default) =>
            await _mapper.ProjectTo<SlayerMasterTable>(_slayerMasterDefinitionRepository.FindAll()).ToListAsync(cancellationToken);
    }
}