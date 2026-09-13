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
        private readonly NpcDefinitionStore _npcDefinitionStore;
        private readonly ITypeProvider<INpcDefinition> _typeProvider;
        private readonly ILogger<NpcService> _logger;

        public NpcService(INpcStore npcStore, NpcDefinitionStore npcDefinitionStore, ITypeProvider<INpcDefinition> typeProvider, ILogger<NpcService> logger)
        {
            _npcStore = npcStore;
            _npcDefinitionStore = npcDefinitionStore;
            _typeProvider = typeProvider;
            _logger = logger;
        }

        public ValueTask<INpc?> FindByIndexAsync(int index) => _npcStore.FindByIndexAsync(index);

        public async Task RegisterAsync(INpc npc)
        {
            EnsureCanRegister(npc);

            bool added;
            try
            {
                added = await _npcStore.AddAsync(npc);
            }
            catch
            {
                DestroyAfterFailedRegistration(npc);
                throw;
            }

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                DestroyAfterFailedRegistration(npc);
                throw failure;
            }

            try
            {
                npc.OnRegistered();
            }
            catch (Exception registrationFailure)
            {
                if (await RemoveAfterFailedRegistrationAsync(npc))
                {
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
            EnsureCanRegister(npc);

            bool added;
            try
            {
                added = _npcStore.Add(npc);
            }
            catch
            {
                DestroyAfterFailedRegistration(npc);
                throw;
            }

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                DestroyAfterFailedRegistration(npc);
                throw failure;
            }

            try
            {
                npc.OnRegistered();
            }
            catch (Exception registrationFailure)
            {
                if (RemoveAfterFailedRegistration(npc))
                {
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

        private static void EnsureCanRegister(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
        }

        private async Task<bool> RemoveAfterFailedRegistrationAsync(INpc npc)
        {
            try
            {
                if (!await _npcStore.RemoveAsync(npc))
                {
                    _logger.LogWarning("Failed to remove NPC '{npc}' from the global store during registration rollback.", npc);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to remove NPC '{npc}' from the global store during registration rollback.", npc);
                return false;
            }
        }

        private bool RemoveAfterFailedRegistration(INpc npc)
        {
            try
            {
                if (!_npcStore.Remove(npc))
                {
                    _logger.LogWarning("Failed to remove NPC '{npc}' from the global store during registration rollback.", npc);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to remove NPC '{npc}' from the global store during registration rollback.", npc);
                return false;
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

            npc.Destroy();
        }

        public INpcDefinition FindNpcDefinitionById(int npcID) => _npcDefinitionStore.GetOrAdd(npcID);

        public int GetNpcDefinitionCount() => _typeProvider.ArchiveSize;
    }
}
