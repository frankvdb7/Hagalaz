using System.Threading.Tasks;
using Nito.AsyncEx;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Metrics;

namespace Hagalaz.Services.GameWorld.Store
{
    public class NpcStore : INpcStore
    {
        private readonly ICreatureCollection<INpc> _npcs = new CreatureCollection<INpc>(short.MaxValue);
        private readonly AsyncReaderWriterLock _lock = new();

        public async ValueTask<bool> AddAsync(INpc npc)
        {
            using (await _lock.WriterLockAsync())
            {
                if (!_npcs.Add(npc))
                {
                    return false;
                }

                GameWorldMetrics.NpcRegistered();
                return true;
            }
        }

        public bool Add(INpc npc)
        {
            using (_lock.WriterLock())
            {
                if (!_npcs.Add(npc))
                {
                    return false;
                }

                GameWorldMetrics.NpcRegistered();
                return true;
            }
        }

        public async ValueTask<bool> RemoveAsync(INpc npc)
        {
            using (await _lock.WriterLockAsync())
            {
                if (!_npcs.Remove(npc))
                {
                    return false;
                }

                GameWorldMetrics.NpcUnregistered();
                return true;
            }
        }

        public bool Remove(INpc npc)
        {
            using (_lock.WriterLock())
            {
                if (!_npcs.Remove(npc))
                {
                    return false;
                }

                GameWorldMetrics.NpcUnregistered();
                return true;
            }
        }

        public async ValueTask<int> CountAsync()
        {
            using (await _lock.ReaderLockAsync())
            {
                return _npcs.Count;
            }
        }

        public async ValueTask<INpc?> FindByIndexAsync(int index)
        {
            if (index < 0 || index > _npcs.Capacity)
            {
                return null;
            }

            using (await _lock.ReaderLockAsync())
            {
                return _npcs[index];
            }
        }
    }
}
