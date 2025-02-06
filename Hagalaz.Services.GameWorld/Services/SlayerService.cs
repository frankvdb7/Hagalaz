using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    public class SlayerService : ISlayerService
    {
        private readonly SlayerStore _slayerStore;

        public SlayerService(SlayerStore slayerStore)
        {
            _slayerStore = slayerStore;
        }

        public async Task<ISlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID)
        {
            await Task.CompletedTask;

            IEnumerable<ISlayerTaskDefinition?> FindTask()
            {
                foreach (var table in _slayerStore.SlayerMasterTables)
                {
                    foreach (var entry in table.Entries)
                    {
                        if (entry.Id == taskID)
                        {
                            yield return entry;
                        }
                    }
                }
            }

            return FindTask().FirstOrDefault();
        }

        public Task<ISlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId) => Task.FromResult<ISlayerMasterTable?>(_slayerStore.SlayerMasterTables.FirstOrDefault(e => e.Id == npcId));

        public Task<IReadOnlyList<ISlayerMasterTable>> FindAllSlayerMasterTables() => Task.FromResult<IReadOnlyList<ISlayerMasterTable>>(_slayerStore.SlayerMasterTables);
    }
}