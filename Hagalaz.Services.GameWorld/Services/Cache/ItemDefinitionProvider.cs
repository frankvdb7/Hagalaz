using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Data;
using System.Linq;

namespace Hagalaz.Services.GameWorld.Services.Cache
{
    public class ItemDefinitionProvider : ITypeProvider<IItemDefinition>
    {
        private readonly ITypeProvider<IItemType> _itemTypeProvider;
        private readonly IItemDefinitionRepository _repository;
        private readonly IMapper _mapper;

        public ItemDefinitionProvider(ITypeProvider<IItemType> itemTypeProvider, IItemDefinitionRepository repository, IMapper mapper)
        {
            _itemTypeProvider = itemTypeProvider;
            _repository = repository;
            _mapper = mapper;
        }

        public int ArchiveSize => _itemTypeProvider.ArchiveSize;

        public IItemDefinition Get(int typeId)
        {
            var itemType = _itemTypeProvider.Get(typeId);
            var itemDefinitionEntity = _repository.FindAll().FirstOrDefault(d => d.Id == (ushort)typeId);

            if (itemDefinitionEntity != null)
            {
                var itemDefinition = _mapper.Map<IItemDefinition>(itemDefinitionEntity);
                _mapper.Map(itemType, itemDefinition);
                return itemDefinition;
            }

            return _mapper.Map<IItemDefinition>(itemType);
        }

        public IItemDefinition[] GetRange(int startTypeId, int endTypeId)
        {
            var definitions = new IItemDefinition[endTypeId - startTypeId];
            for (var i = 0; i < definitions.Length; i++)
            {
                var typeId = startTypeId + i;
                definitions[i] = Get(typeId);
            }
            return definitions;
        }

        public IItemDefinition[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}