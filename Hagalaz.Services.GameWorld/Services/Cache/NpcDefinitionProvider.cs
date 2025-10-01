using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Data;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Services.Cache
{
    public class NpcDefinitionProvider : ITypeProvider<INpcDefinition>
    {
        private readonly ITypeProvider<INpcType> _npcTypeProvider;
        private readonly INpcDefinitionRepository _repository;
        private readonly IMapper _mapper;

        public NpcDefinitionProvider(ITypeProvider<INpcType> npcTypeProvider, INpcDefinitionRepository repository, IMapper mapper)
        {
            _npcTypeProvider = npcTypeProvider;
            _repository = repository;
            _mapper = mapper;
        }

        public int ArchiveSize => _npcTypeProvider.ArchiveSize;

        public INpcDefinition Get(int typeId)
        {
            var npcType = _npcTypeProvider.Get(typeId);
            var npcDefinitionEntity = _repository.FindAll().FirstOrDefault(d => d.NpcId == (ushort)typeId);

            if (npcDefinitionEntity != null)
            {
                var npcDefinition = _mapper.Map<INpcDefinition>(npcDefinitionEntity);
                _mapper.Map(npcType, npcDefinition);
                return npcDefinition;
            }

            return _mapper.Map<INpcDefinition>(npcType);
        }

        public INpcDefinition[] GetRange(int startTypeId, int endTypeId)
        {
            var npcTypes = _npcTypeProvider.GetRange(startTypeId, endTypeId);
            var typeIds = Enumerable.Range(startTypeId, endTypeId - startTypeId).Select(id => (ushort)id).ToHashSet();
            var npcDefinitionEntities = _repository.FindAll()
                .Where(d => typeIds.Contains(d.NpcId))
                .ToDictionary(d => d.NpcId);

            var definitions = new INpcDefinition[npcTypes.Length];
            for (var i = 0; i < npcTypes.Length; i++)
            {
                var npcType = npcTypes[i];
                var typeId = (ushort)npcType.Id;

                if (npcDefinitionEntities.TryGetValue(typeId, out var npcDefinitionEntity))
                {
                    var npcDefinition = _mapper.Map<INpcDefinition>(npcDefinitionEntity);
                    _mapper.Map(npcType, npcDefinition);
                    definitions[i] = npcDefinition;
                }
                else
                {
                    definitions[i] = _mapper.Map<INpcDefinition>(npcType);
                }
            }
            return definitions;
        }

        public INpcDefinition[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}