using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class ItemService : IItemService
    {
        private readonly ItemStore _items;
        private readonly ITypeDecoder<IItemDefinition> _itemDecoder;

        public ItemService(ItemStore items, ITypeDecoder<IItemDefinition> itemDecoder)
        {
            _items = items;
            _itemDecoder = itemDecoder;
        }


        public IItemDefinition FindItemDefinitionById(int itemId) => _items.GetOrAdd(itemId);

        public int GetTotalItemCount() => _itemDecoder.ArchiveSize;
    }
}