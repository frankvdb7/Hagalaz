using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Model.Items;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class ItemBuilder : IItemBuilder, IItemBuild, IItemId, IItemOptional
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IItemScriptProvider _itemScriptProvider;
        private readonly IEquipmentScriptProvider _equipmentScriptProvider;
        private int _id;
        private int _count = 1;
        private string? _extraData;

        public ItemBuilder(IServiceProvider serviceProvider, IItemScriptProvider itemScriptProvider, IEquipmentScriptProvider equipmentScriptProvider)
        {
            _serviceProvider = serviceProvider;
            _itemScriptProvider = itemScriptProvider;
            _equipmentScriptProvider = equipmentScriptProvider;
        }

        public IItemId Create() => new ItemBuilder(_serviceProvider.CreateScope().ServiceProvider, _itemScriptProvider, _equipmentScriptProvider);

        public IItem Build()
        {
            var itemDefinition = _serviceProvider.GetRequiredService<IItemService>().FindItemDefinitionById(_id);
            var equipmentDefinition = _serviceProvider.GetRequiredService<IEquipmentService>().FindEquipmentDefinitionById(_id);
            var itemScript = _itemScriptProvider.FindItemScriptById(_id);
            var equipmentScript = _equipmentScriptProvider.FindEquipmentScriptById(_id);
            var item = new Item(_id, _count, itemDefinition, equipmentDefinition, itemScript, equipmentScript);
            if (!string.IsNullOrWhiteSpace(_extraData))
            {
                item.UnserializeExtraData(_extraData);
            }
            return new Item(_id, _count, itemDefinition, equipmentDefinition, itemScript, equipmentScript);
        }

        public IItemOptional WithCount(int count)
        {
            _count = count;
            return this;
        }

        public IItemOptional WithExtraData(string data)
        {
            _extraData = data;
            return this;
        }

        public IItemOptional WithId(int id)
        {
            _id = id;
            return this;
        }
    }
}