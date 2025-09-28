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

        public IItemDefinition Decode(int typeId)
        {
            var itemType = _itemTypeProvider.Decode(typeId);
            var itemDefinitionEntity = _repository.FindAll().FirstOrDefault(d => d.Id == (ushort)typeId);

            var itemDefinition = _mapper.Map<IItemDefinition>(itemType);
            if (itemDefinitionEntity != null)
            {
                _mapper.Map(itemDefinitionEntity, itemDefinition);
            }

            return itemDefinition;
        }

        public IItemDefinition[] DecodeRange(int startTypeId, int endTypeId)
        {
            var types = new IItemDefinition[endTypeId - startTypeId];
            var itemDefinitions = _repository.FindAll().Where(d => d.Id >= startTypeId && d.Id < endTypeId).ToDictionary(d => d.Id, d => d);

            for (var i = 0; i < types.Length; i++)
            {
                var typeId = startTypeId + i;
                var itemType = _itemTypeProvider.Decode(typeId);
                var itemDefinition = _mapper.Map<IItemDefinition>(itemType);

                if (itemDefinitions.TryGetValue((ushort)typeId, out var itemDefinitionEntity))
                {
                    _mapper.Map(itemDefinitionEntity, itemDefinition);
                }

                types[i] = itemDefinition;
            }
            return types;
        }

        public IItemDefinition[] DecodeAll()
        {
            var count = ArchiveSize;
            return DecodeRange(0, count);
        }
    }
}