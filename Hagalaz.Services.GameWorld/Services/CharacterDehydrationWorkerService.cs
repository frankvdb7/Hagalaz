using System;
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
            await base.StopAsync(cancellationToken);

            try
            {
                _logger.LogInformation("Flushing active character snapshots before shutdown");
                await FlushAsync(force: true, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("Character snapshot shutdown flush was canceled before completion");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Character snapshot shutdown flush failed");
            }
        }

        private async Task FlushAsync(bool force, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var persistenceService = scope.ServiceProvider.GetRequiredService<ICharacterPersistenceService>();
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
                    _logger.LogError(exception,
                        "Failed to persist character {MasterId}; it will be retried on the next flush",
                        character.MasterId);
                }
            });
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
