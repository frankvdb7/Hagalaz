using System;
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
        private readonly IEntityStore _entityStore;
        private readonly NpcDefinitionStore _npcDefinitionStore;
        private readonly ITypeProvider<INpcDefinition> _typeProvider;
        private readonly ILogger<NpcService> _logger;
        public NpcService(
            INpcStore npcStore,
            IEntityStore entityStore,
            NpcDefinitionStore npcDefinitionStore,
            ITypeProvider<INpcDefinition> typeProvider,
            ILogger<NpcService> logger)
        {
            _npcStore = npcStore;
            _entityStore = entityStore;
            _npcDefinitionStore = npcDefinitionStore;
            _typeProvider = typeProvider;
            _logger = logger;
        }

        public ValueTask<INpc?> FindByIndexAsync(int index) => _npcStore.FindByIndexAsync(index);

        public async Task RegisterAsync(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            var added = await _npcStore.AddAsync(npc);

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                DestroyAfterFailedRegistration(npc);
                throw failure;
            }

            try
            {
                _entityStore.Add(npc);
                npc.OnRegistered();
            }
            catch (Exception registrationFailure)
            {
                if (await _npcStore.RemoveAsync(npc))
                {
                    RemoveIdentity(npc, registrationFailure);
                    DestroyAfterFailedRegistration(npc);
                }
                else
                {
                    _logger.LogError(registrationFailure,
                        "NPC '{npc}' registration failed and global ownership could not be released; the NPC was not destroyed.",
                        npc);
                }
                throw;
            }
        }

        public void Register(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            var added = _npcStore.Add(npc);

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                DestroyAfterFailedRegistration(npc);
                throw failure;
            }

            try
            {
                _entityStore.Add(npc);
                npc.OnRegistered();
            }
            catch (Exception registrationFailure)
            {
                if (_npcStore.Remove(npc))
                {
                    RemoveIdentity(npc, registrationFailure);
                    DestroyAfterFailedRegistration(npc);
                }
                else
                {
                    _logger.LogError(registrationFailure,
                        "NPC '{npc}' registration failed and global ownership could not be released; the NPC was not destroyed.",
                        npc);
                }
                throw;
            }
        }

        private void DestroyAfterFailedRegistration(INpc npc)
        {
            try
            {
                npc.Destroy();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to destroy NPC '{npc}' after registration failed.", npc);
            }
        }

        public async Task UnregisterAsync(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            if (!await _npcStore.RemoveAsync(npc))
            {
                _logger.LogDebug("NPC '{npc}' was already absent from the global store during unregister.", npc);
                return;
            }

            RemoveIdentity(npc, null);
            npc.Destroy();
        }

        public void Unregister(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            if (!_npcStore.Remove(npc))
            {
                _logger.LogDebug("NPC '{npc}' was already absent from the global store during unregister.", npc);
                return;
            }

            RemoveIdentity(npc, null);
            npc.Destroy();
        }

        private void RemoveIdentity(INpc npc, Exception? relatedFailure)
        {
            if (_entityStore.Remove(npc))
            {
                return;
            }

            _logger.LogError(relatedFailure,
                "NPC '{npc}' was removed from the global store but had no matching entity identity.", npc);
        }

        public INpcDefinition FindNpcDefinitionById(int npcID) => _npcDefinitionStore.GetOrAdd(npcID);

        public int GetNpcDefinitionCount() => _typeProvider.ArchiveSize;
    }
}
