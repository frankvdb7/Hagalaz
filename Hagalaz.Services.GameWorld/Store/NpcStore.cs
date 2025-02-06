using System.Collections.Generic;
using System.Threading.Tasks;
using Nito.AsyncEx;
using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Store
{
    public class NpcStore : INpcStore
    {
        private readonly ICreatureCollection<INpc> _npcs = new CreatureCollection<INpc>(short.MaxValue);
        private readonly AsyncReaderWriterLock _lock = new();

        public async IAsyncEnumerable<INpc> FindAllAsync()
        {
            using (await _lock.ReaderLockAsync())
            {
                foreach (var npc in _npcs)
                {
                    yield return npc;
                }
            }
        }

        public async ValueTask<bool> AddAsync(INpc npc)
        {
            using (await _lock.WriterLockAsync())
            {
                return _npcs.Add(npc);
            }
        }

        public async ValueTask<bool> RemoveAsync(INpc npc)
        {
            using (await _lock.WriterLockAsync())
            {
                return _npcs.Remove(npc);
            }
        }

        public async ValueTask<int> CountAsync()
        {
            using (await _lock.ReaderLockAsync())
            {
                return _npcs.Count;
            }
        }

        public ValueTask<INpc?> FindAsync(Func<INpc, bool> predicate) => FindAllAsync().Where(predicate).FirstOrDefaultAsync();
    }
}