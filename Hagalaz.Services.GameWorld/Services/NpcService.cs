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
            EnsureCanRegister(npc);

            bool added;
            try
            {
                added = await _npcStore.AddAsync(npc);
            }
            catch (Exception exception)
            {
                RethrowRegistrationFailure(exception, CleanupUnpublishedNpc(npc));
                return;
            }

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                RethrowRegistrationFailure(failure, CleanupUnpublishedNpc(npc));
                return;
            }

            try
            {
                npc.OnRegistered();
            }
            catch (Exception exception)
            {
                RethrowRegistrationFailure(exception, await CleanupRegisteredNpcAsync(npc));
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
            catch (Exception exception)
            {
                RethrowRegistrationFailure(exception, CleanupUnpublishedNpc(npc));
                return;
            }

            if (!added)
            {
                var failure = new InvalidOperationException($"Failed to add NPC '{npc}' to the global store.");
                _logger.LogWarning(failure, "Failed to add NPC '{npc}' to the global store.", npc);
                RethrowRegistrationFailure(failure, CleanupUnpublishedNpc(npc));
                return;
            }

            try
            {
                npc.OnRegistered();
            }
            catch (Exception exception)
            {
                RethrowRegistrationFailure(exception, CleanupRegisteredNpc(npc));
            }
        }

        private static void EnsureCanRegister(INpc npc)
        {
            ArgumentNullException.ThrowIfNull(npc);
            if (npc.IsDestroyed)
            {
                throw new InvalidOperationException($"Cannot register destroyed NPC '{npc}'.");
            }
        }

        private Exception? CleanupUnpublishedNpc(INpc npc) =>
            CreateCleanupFailure(npc, removalFailure: null, destroyFailure: DestroyNpcSafely(npc));

        private async Task<Exception?> CleanupRegisteredNpcAsync(INpc npc) =>
            CreateCleanupFailure(npc, await RemoveNpcForRegistrationRollbackAsync(npc), destroyFailure: DestroyNpcSafely(npc));

        private Exception? CleanupRegisteredNpc(INpc npc) =>
            CreateCleanupFailure(npc, RemoveNpcForRegistrationRollback(npc), destroyFailure: DestroyNpcSafely(npc));

        private async Task<Exception?> RemoveNpcForRegistrationRollbackAsync(INpc npc)
        {
            try
            {
                if (!await _npcStore.RemoveAsync(npc))
                {
                    return new InvalidOperationException($"Failed to remove NPC '{npc}' from the global store during registration rollback.");
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private Exception? RemoveNpcForRegistrationRollback(INpc npc)
        {
            try
            {
                if (!_npcStore.Remove(npc))
                {
                    return new InvalidOperationException($"Failed to remove NPC '{npc}' from the global store during registration rollback.");
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private Exception? DestroyNpcSafely(INpc npc)
        {
            if (npc.IsDestroyed)
            {
                return null;
            }

            try
            {
                npc.Destroy();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private Exception? CreateCleanupFailure(INpc npc, Exception? removalFailure, Exception? destroyFailure)
        {
            List<Exception>? failures = null;

            if (removalFailure is not null)
            {
                (failures ??= []).Add(removalFailure);
            }

            if (destroyFailure is not null)
            {
                (failures ??= []).Add(destroyFailure);
            }

            if (failures is null)
            {
                return null;
            }

            var cleanupFailure = new AggregateException("Failed to clean up the unregistered NPC.", failures);
            _logger.LogError(cleanupFailure, "Failed to clean up NPC '{npc}' after registration failed.", npc);
            return cleanupFailure;
        }

        private static void RethrowRegistrationFailure(Exception registrationFailure, Exception? cleanupFailure)
        {
            if (cleanupFailure is not null)
            {
                throw new AggregateException("NPC registration and cleanup both failed.", registrationFailure, cleanupFailure);
            }

            ExceptionDispatchInfo.Capture(registrationFailure).Throw();
        }

        public async Task UnregisterAsync(INpc npc)
        {
            var destroyFailure = DestroyNpcSafely(npc);
            var removalFailure = await RemoveNpcAsync(npc);
            RethrowUnregisterFailures(destroyFailure, removalFailure);
        }

        public void Unregister(INpc npc)
        {
            var destroyFailure = DestroyNpcSafely(npc);
            var removalFailure = RemoveNpc(npc);
            RethrowUnregisterFailures(destroyFailure, removalFailure);
        }

        private async Task<Exception?> RemoveNpcAsync(INpc npc)
        {
            try
            {
                if (!await _npcStore.RemoveAsync(npc))
                {
                    _logger.LogWarning("Failed to remove npc '{npc}'", npc);
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private Exception? RemoveNpc(INpc npc)
        {
            try
            {
                if (!_npcStore.Remove(npc))
                {
                    _logger.LogWarning("Failed to remove npc '{npc}'", npc);
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static void RethrowUnregisterFailures(Exception? destroyFailure, Exception? removalFailure)
        {
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
