using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public class CharacterDehydrationWorkerService : BackgroundService
    {
        private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(30);
        private readonly ILogger<CharacterDehydrationWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICharacterStore _characterStore;
        private readonly TimeSpan _shutdownTimeout;

        public CharacterDehydrationWorkerService(
            ILogger<CharacterDehydrationWorkerService> logger,
            IServiceProvider serviceProvider,
            ICharacterStore characterStore)
            : this(logger, serviceProvider, characterStore, DefaultShutdownTimeout)
        {
        }

        internal CharacterDehydrationWorkerService(
            ILogger<CharacterDehydrationWorkerService> logger,
            IServiceProvider serviceProvider,
            ICharacterStore characterStore,
            TimeSpan shutdownTimeout)
        {
            if (shutdownTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout));
            }

            _logger = logger;
            _serviceProvider = serviceProvider;
            _characterStore = characterStore;
            _shutdownTimeout = shutdownTimeout;
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
            using var shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            shutdownCts.CancelAfter(_shutdownTimeout);
            var shutdownToken = shutdownCts.Token;

            try
            {
                await base.StopAsync(shutdownToken);
                _logger.LogInformation("Flushing active character snapshots before shutdown");
                await FlushAsync(force: true, shutdownToken);
            }
            catch (OperationCanceledException exception) when (shutdownToken.IsCancellationRequested)
            {
                _logger.LogCritical(exception,
                    "Character snapshot shutdown exceeded the {ShutdownTimeout} deadline; durable handoff remains incomplete",
                    _shutdownTimeout);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Character snapshot shutdown flush failed; durable handoff remains incomplete");
                throw;
            }
        }

        internal async Task FlushAsync(bool force, CancellationToken cancellationToken)
        {
            var characters = new List<ICharacter>((await _characterStore.GetSnapshotAsync(cancellationToken)).Values);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            };
            var failures = new ConcurrentBag<Exception>();
            await Parallel.ForEachAsync(characters, options, async (character, token) =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                try
                {
                    var persistenceService = scope.ServiceProvider.GetRequiredService<ICharacterPersistenceService>();
                    var logoutService = scope.ServiceProvider.GetRequiredService<ICharacterLogoutService>();
                    if (logoutService.IsPendingLogout(character))
                    {
                        return;
                    }

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
                            "Failed to queue character {MasterId} in the EF bus outbox; it will be retried on the next flush",
                            character.MasterId);
                    }
                }
            });

            if (force && !failures.IsEmpty)
            {
                throw new AggregateException("One or more character snapshots could not be durably handed off during shutdown.", failures);
            }
        }

    }
}
