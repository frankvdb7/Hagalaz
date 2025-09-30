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
            var itemTypes = _itemTypeProvider.GetRange(startTypeId, endTypeId);
            var typeIds = Enumerable.Range(startTypeId, endTypeId - startTypeId).Select(id => (ushort)id).ToHashSet();
            var itemDefinitionEntities = _repository.FindAll()
                .Where(d => typeIds.Contains(d.Id))
                .ToDictionary(d => d.Id);

            var definitions = new IItemDefinition[itemTypes.Length];
            for (var i = 0; i < itemTypes.Length; i++)
            {
                var itemType = itemTypes[i];
                var typeId = (ushort)itemType.Id;

                if (itemDefinitionEntities.TryGetValue(typeId, out var itemDefinitionEntity))
                {
                    var itemDefinition = _mapper.Map<IItemDefinition>(itemDefinitionEntity);
                    _mapper.Map(itemType, itemDefinition);
                    definitions[i] = itemDefinition;
                }
                else
                {
                    definitions[i] = _mapper.Map<IItemDefinition>(itemType);
                }
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