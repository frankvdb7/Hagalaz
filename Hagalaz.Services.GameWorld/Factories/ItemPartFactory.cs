using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class ItemPartFactory : IItemPartFactory
    {
        private readonly IItemService _itemService;

        public ItemPartFactory(IItemService itemService) => _itemService = itemService;

        public IItemPart Create(int itemId)
        {
            var item = _itemService.FindItemDefinitionById(itemId);
            return new ItemPart(itemId)
            {
                MaleModels = [item.MaleWornModelId1, item.MaleWornModelId2, item.MaleWornModelId3],
                FemaleModels = [item.FemaleWornModelId1, item.FemaleWornModelId2, item.FemaleWornModelId3],
                ModelColors = item.ModifiedModelColors != null ? [..item.ModifiedModelColors] : [],
                TextureColors = item.ModifiedTextureColors != null ? [..item.ModifiedTextureColors] : [],
            };
        }
    }
}