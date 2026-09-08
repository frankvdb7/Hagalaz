using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

        public ValueTask<INpc?> FindByIndexAsync(int index) => _npcStore.FindAsync(npc => npc.Index == index);

        public async Task RegisterAsync(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            if (npc.IsDestroyed)
            {
                throw new InvalidOperationException($"Cannot register destroyed NPC '{npc}'.");
            }

            bool added;
            try
            {
                added = await _npcStore.AddAsync(npc);
            }
            catch (Exception exception)
            {
                var cleanupFailure = await CleanupUnregisteredNpcAsync(npc, storePublished: false);
                if (cleanupFailure is not null)
                {
                    throw new AggregateException("NPC registration and cleanup both failed.", exception, cleanupFailure);
                }

                throw;
            }

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                var cleanupFailure = await CleanupUnregisteredNpcAsync(npc, storePublished: false);
                if (cleanupFailure is not null)
                {
                    throw new AggregateException("NPC registration and cleanup both failed.", failure, cleanupFailure);
                }

                throw failure;
            }

            try
            {
                await npc.OnRegistered();
            }
            catch (Exception exception)
            {
                var cleanupFailure = await CleanupUnregisteredNpcAsync(npc, storePublished: true);
                if (cleanupFailure is not null)
                {
                    throw new AggregateException("NPC registration and cleanup both failed.", exception, cleanupFailure);
                }

                throw;
            }
        }

        private async Task<Exception?> CleanupUnregisteredNpcAsync(INpc npc, bool storePublished)
        {
            List<Exception>? failures = null;

            if (storePublished)
            {
                try
                {
                    if (!await _npcStore.RemoveAsync(npc))
                    {
                        (failures ??= []).Add(new InvalidOperationException($"Failed to remove NPC '{npc}' from the global store during registration rollback."));
                    }
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }
            }

            try
            {
                if (!npc.IsDestroyed)
                {
                    npc.Destroy();
                }
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }

            if (failures is null)
            {
                return null;
            }

            var cleanupFailure = new AggregateException("Failed to clean up the unregistered NPC.", failures);
            _logger.LogError(cleanupFailure, "Failed to clean up NPC '{npc}' after registration failed.", npc);
            return cleanupFailure;
        }

        public async Task UnregisterAsync(INpc npc)
        {
            Exception? destroyFailure = null;
            if (!npc.IsDestroyed)
            {
                try
                {
                    npc.Destroy();
                }
                catch (Exception exception)
                {
                    destroyFailure = exception;
                }
            }

            Exception? removalFailure = null;
            try
            {
                if (!await _npcStore.RemoveAsync(npc))
                {
                    _logger.LogWarning("Failed to remove npc '{npc}'", npc);
                }
            }
            catch (Exception exception)
            {
                removalFailure = exception;
            }

            if (destroyFailure is not null && removalFailure is not null)
            {
                throw new AggregateException("NPC destruction and global-store removal both failed.", destroyFailure, removalFailure);
            }

            if (destroyFailure is not null)
            {
                ExceptionDispatchInfo.Capture(destroyFailure).Throw();
            }

            if (removalFailure is not null)
            {
                ExceptionDispatchInfo.Capture(removalFailure).Throw();
            }
        }

        public INpcDefinition FindNpcDefinitionById(int npcID) => _npcDefinitionStore.GetOrAdd(npcID);

        public int GetNpcDefinitionCount() => _typeProvider.ArchiveSize;
    }
}
