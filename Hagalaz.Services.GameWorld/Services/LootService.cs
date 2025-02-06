using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class LootService : ILootService
    {
        private readonly LootStore _lootStore;

        public LootService(LootStore lootStore)
        {
            _lootStore = lootStore;
        }

        public async Task<ILootTable?> FindGameObjectLootTable(int id)
        {
            await Task.CompletedTask;
            return _lootStore.TryGetGameObjectLootTable(id, out var table) ? table : null;
        }

        public async Task<ILootTable?> FindItemLootTable(int id)
        {
            await Task.CompletedTask;
            return _lootStore.TryGetItemLootTable(id, out var table) ? table : null;
        }

        public async Task<ILootTable?> FindNpcLootTable(int id)
        {
            await Task.CompletedTask;
            return _lootStore.TryGetNpcLootTable(id, out var table) ? table : null;
        }
    }
}
