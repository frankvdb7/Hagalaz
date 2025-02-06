using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Store
{
    public class GameObjectStore : ConcurrentStore<int, IGameObjectDefinition>, IStartupService
    {
        private Dictionary<int, Hagalaz.Data.Entities.GameobjectDefinition> _databaseItems = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeDecoder<IGameObjectDefinition> _objectDecoder;

        public GameObjectStore(IServiceProvider serviceProvider, ITypeDecoder<IGameObjectDefinition> objectDecoder)
        {
            _serviceProvider = serviceProvider;
            _objectDecoder = objectDecoder;
        }

        private IGameObjectDefinition LoadObjectDefinition(int objectId)
        {
            var definition = _objectDecoder.Decode(objectId);
            if (_databaseItems.TryGetValue(objectId, out var obj))
            {
                definition.Examine = obj.Examine;
                definition.LootTableId = obj.GameobjectLootId ?? 0;
            }
            return definition;
        }

        public IGameObjectDefinition GetOrAdd(int objectId) => GetOrAdd(objectId, LoadObjectDefinition);

        // TODO - implement Redis for this
        public async Task LoadAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            _databaseItems = (await scope.ServiceProvider.GetRequiredService<IGameObjectDefinitionRepository>().FindAll().AsNoTracking().ToListAsync()).ToDictionary(l => (int)l.GameobjectId);
        }
    }
}