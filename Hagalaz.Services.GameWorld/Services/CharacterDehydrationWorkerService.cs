using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterDehydrationWorkerService : BackgroundService
    {
        private readonly ILogger<CharacterDehydrationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICharacterStore _characterStore;

        public CharacterDehydrationWorkerService(
            ILogger<CharacterDehydrationWorkerService> logger,
            IServiceProvider serviceProvider,
            ICharacterStore characterStore)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _characterStore = characterStore;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ReplayPendingAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                    await FlushAsync(force: false, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error occurred dehydrating characters");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Let the worker finish its cancellation path before flushing. The flush itself must
            // not use the host timeout token because the durable handoff is the final data-safety step.
            await base.StopAsync(CancellationToken.None);

            try
            {
                _logger.LogInformation("Flushing active character snapshots before shutdown");
                await FlushAsync(force: true, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Character snapshot shutdown flush failed; host shutdown cannot safely continue");
                throw;
            }
        }

        private async Task FlushAsync(bool force, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var persistenceService = scope.ServiceProvider.GetRequiredService<ICharacterPersistenceService>();
            await persistenceService.ReplayPendingAsync(cancellationToken);
            var characters = new List<ICharacter>();
            await foreach (var character in _characterStore.FindAllAsync().WithCancellation(cancellationToken))
            {
                characters.Add(character);
            }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            };
            var failures = new ConcurrentBag<Exception>();
            await Parallel.ForEachAsync(characters, options, async (character, token) =>
            {
                try
                {
                    await persistenceService.PersistAsync(character, force, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                    if (!force)
                    {
                        _logger.LogError(exception,
                            "Failed to persist character {MasterId}; it will be retried on the next flush or from the durable outbox",
                            character.MasterId);
                    }
                }
            });

            if (force && !failures.IsEmpty)
            {
                throw new AggregateException("One or more character snapshots could not be durably handed off during shutdown.", failures);
            }
        }

        private async Task ReplayPendingAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<ICharacterPersistenceService>()
                .ReplayPendingAsync(cancellationToken);
        }

        // Kept as a compatibility helper for existing dehydration request tests.
        internal static DehydrateCharacter CreateRequest(IMapper mapper, Services.Model.CharacterModel model, uint masterId, long snapshotRevision) =>
            mapper.Map<DehydrateCharacter>(model) with
            {
                MasterId = masterId,
                CorrelationId = Guid.NewGuid(),
                SnapshotRevision = snapshotRevision
            };
    }
}
