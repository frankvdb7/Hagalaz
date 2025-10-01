using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class ItemService : IItemService
    {
        private readonly ItemStore _items;
        private readonly ITypeProvider<IItemDefinition> _itemProvider;

        public ItemService(ItemStore items, ITypeProvider<IItemDefinition> itemProvider)
        {
            _items = items;
            _itemProvider = itemProvider;
        }


        public IItemDefinition FindItemDefinitionById(int itemId) => _items.GetOrAdd(itemId);

        public int GetTotalItemCount() => _itemProvider.ArchiveSize;
    }
}