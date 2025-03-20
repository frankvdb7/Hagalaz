using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Loot;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Store
{
    public class LootStore : IStartupService
    {
        private readonly IServiceProvider _serviceProvider;
        private Dictionary<int, LootTable> _gameObjectLootTables = default!;
        private Dictionary<int, LootTable> _itemLootTables = default!;
        private Dictionary<int, LootTable> _npcLootTables = default!;

        public LootStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool TryGetGameObjectLootTable(int id, out LootTable? table) => _gameObjectLootTables.TryGetValue(id, out table);
        public bool TryGetItemLootTable(int id, out LootTable? table) => _itemLootTables.TryGetValue(id, out table);
        public bool TryGetNpcLootTable(int id, out LootTable? table) => _npcLootTables.TryGetValue(id, out table);

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();

            var lootModifierProvider = scope.ServiceProvider.GetRequiredService<ILootModifierProvider>();
            var lootModifiers = lootModifierProvider.FindLootModifiers().ToList();

            var gameObjectLootTables = await scope.ServiceProvider.GetRequiredService<IGameObjectLootRepository>()
                .FindAll()
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            _gameObjectLootTables = gameObjectLootTables.Select(t => new LootTable()
                {
                    Id = t.Id,
                    Name = t.Name,
                    Entries = t.GameobjectLootItems.Select(i => new LootItem(i.ItemId,
                            (int)i.MinimumCount,
                            (int)i.MaximumCount,
                            (double)i.Probability,
                            i.Always == 1))
                        .ToList<ILootObject>(),
                    Modifiers = lootModifiers,
                    MaxResultCount = (int)t.MaximumLootCount,
                    RandomizeResultCount = t.RandomizeLootCount == 1
                })
                .ToDictionary(e => e.Id);

            var itemLootTables = await scope.ServiceProvider.GetRequiredService<IItemLootRepository>()
                .FindAll()
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            _itemLootTables = itemLootTables.Select(t => new LootTable()
                {
                    Id = t.Id,
                    Name = t.Name,
                    Entries = t.ItemLootItems.Select(i => new LootItem(i.ItemId,
                            (int)i.MinimumCount,
                            (int)i.MaximumCount,
                            (double)i.Probability,
                            i.Always == 1))
                        .ToList<ILootObject>(),
                    Modifiers = lootModifiers,
                    MaxResultCount = (int)t.MaximumLootCount,
                    RandomizeResultCount = t.RandomizeLootCount == 1
                })
                .ToDictionary(e => e.Id);

            var npcLootTables = await scope.ServiceProvider.GetRequiredService<INpcLootRepository>()
                .FindAll()
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            _npcLootTables = npcLootTables.Select(t => new LootTable()
                {
                    Id = t.Id,
                    Name = t.Name,
                    Entries = t.NpcLootItems.Select(i => new LootItem(i.ItemId,
                            (int)i.MinimumCount,
                            (int)i.MaximumCount,
                            (double)i.Probability,
                            i.Always == 1))
                        .ToList<ILootObject>(),
                    Modifiers = lootModifiers
                })
                .ToDictionary(e => e.Id);

            foreach (var t in gameObjectLootTables)
            {
                if (!_gameObjectLootTables.TryGetValue(t.Id, out var table))
                {
                    continue;
                }

                var children = t.GameobjectLootChildren.Select(l => new
                    {
                        l.Id
                    })
                    .ToList();
                foreach (var child in children)
                {
                    if (!_gameObjectLootTables.TryGetValue(child.Id, out var childTable))
                    {
                        continue;
                    }

                    table.AddEntry(childTable);
                }
            }

            foreach (var t in itemLootTables)
            {
                if (!_itemLootTables.TryGetValue(t.Id, out var table))
                {
                    continue;
                }

                var children = t.ItemLootChildren.Select(l => new
                    {
                        l.Id
                    })
                    .ToList();
                foreach (var child in children)
                {
                    if (!_itemLootTables.TryGetValue(child.Id, out var childTable))
                    {
                        continue;
                    }

                    table.AddEntry(childTable);
                }
            }

            foreach (var t in npcLootTables)
            {
                if (!_npcLootTables.TryGetValue(t.Id, out var table))
                {
                    continue;
                }

                var children = t.NpcLootChildren.Select(l => new
                    {
                        l.Id
                    })
                    .ToList();
                foreach (var child in children)
                {
                    if (!_npcLootTables.TryGetValue(child.Id, out var childTable))
                    {
                        continue;
                    }

                    table.AddEntry(childTable);
                }
            }

            var allLootTables = _npcLootTables.Values.Union(_itemLootTables.Values).Union(_gameObjectLootTables.Values);
            foreach (var lootTable in allLootTables)
            {
                if (lootTable.Name.Contains("rare", StringComparison.OrdinalIgnoreCase))
                {
                    lootTable.AddModifier(new RingOfWealthModifier());
                }
            }
        }
    }
}