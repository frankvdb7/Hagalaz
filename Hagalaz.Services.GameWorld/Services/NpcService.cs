using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class NpcService : INpcService
    {
        private readonly INpcStore _npcStore;
        private readonly NpcDefinitionStore _npcDefinitionStore;
        private readonly ITypeDecoder<INpcDefinition> _typeDecoder;
        private readonly ILogger<NpcService> _logger;

        public NpcService(INpcStore npcStore, NpcDefinitionStore npcDefinitionStore, ITypeDecoder<INpcDefinition> typeDecoder, ILogger<NpcService> logger)
        {
            _npcStore = npcStore;
            _npcDefinitionStore = npcDefinitionStore;
            _typeDecoder = typeDecoder;
            _logger = logger;
        }

        public ValueTask<INpc?> FindByIndexAsync(int index) => _npcStore.FindAsync(npc => npc.Index == index);

        public async Task RegisterAsync(INpc npc)
        {
            if (!await _npcStore.AddAsync(npc))
            {
                _logger.LogWarning("Failed to add npc '{npc}'", npc);
                return;
            }

            await npc.OnRegistered();
        }

        public async Task UnregisterAsync(INpc npc)
        {
            if (npc.IsDestroyed)
            {
                _logger.LogWarning("Failed to unregister destroyed npc '{npc}'", npc);
                return;
            }

            npc.Destroy();

            if (!await _npcStore.RemoveAsync(npc))
            {
                _logger.LogWarning("Failed to remove npc '{npc}'", npc);
            }
        }

        public INpcDefinition FindNpcDefinitionById(int npcID) => _npcDefinitionStore.GetOrAdd(npcID);

        public int GetNpcDefinitionCount() => _typeDecoder.ArchiveSize;
    }
}