using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Store
{
    public class ItemStore : ConcurrentStore<int, IItemDefinition>, IStartupService
    {
        private Dictionary<int, Hagalaz.Data.Entities.ItemDefinition> _databaseItems = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeDecoder<IItemDefinition> _itemDecoder;
        private readonly ILogger<ItemStore> _logger;

        public ItemStore(IServiceProvider serviceProvider, ITypeDecoder<IItemDefinition> itemDecoder, ILogger<ItemStore> logger)
        {
            _serviceProvider = serviceProvider;
            _itemDecoder = itemDecoder;
            _logger = logger;
        }

        private IItemDefinition LoadItemDefinition(int itemId)
        {
            var definition = _itemDecoder.Decode(itemId);
            if (_databaseItems.TryGetValue(itemId, out var item))
            {
                definition.Examine = item.Examine;
                definition.TradeValue = item.TradePrice;
                definition.LowAlchemyValue = item.LowAlchemyValue;
                definition.HighAlchemyValue = item.HighAlchemyValue;
                definition.Tradeable = item.Tradeable == 1;
                definition.Weight = (double)item.Weight;
            }
            return definition;
        }

        public IItemDefinition GetOrAdd(int itemId) => GetOrAdd(itemId, LoadItemDefinition);

        // TODO - implement Redis for this
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            _databaseItems = (await scope.ServiceProvider.GetRequiredService<IItemDefinitionRepository>().FindAll().AsNoTracking().ToListAsync()).ToDictionary(l => (int)l.Id);

            _logger.LogInformation("Loaded {Count} items definitions", _databaseItems.Count);
        }
    }
}
