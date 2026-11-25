using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Metrics;
using Hagalaz.Services.GameWorld.Services.Model;
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
        private readonly IRsTaskService _taskScheduler;
        private readonly TimeSpan _shutdownTimeout;

        public CharacterDehydrationWorkerService(
            ILogger<CharacterDehydrationWorkerService> logger,
            IServiceProvider serviceProvider,
            ICharacterStore characterStore,
            IRsTaskService taskScheduler)
            : this(logger, serviceProvider, characterStore, taskScheduler, DefaultShutdownTimeout)
        {
        }

        internal CharacterDehydrationWorkerService(
            ILogger<CharacterDehydrationWorkerService> logger,
            IServiceProvider serviceProvider,
            ICharacterStore characterStore,
            IRsTaskService taskScheduler,
            TimeSpan shutdownTimeout)
        {
            if (shutdownTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout));
            }

            _logger = logger;
            _serviceProvider = serviceProvider;
            _characterStore = characterStore;
            _taskScheduler = taskScheduler;
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

        // One attempt is one flush: the result reflects the hand-off to the persistence service,
        // not the asynchronous database commit, and any failed character makes the attempt a failure.
        internal async Task FlushAsync(bool force, CancellationToken cancellationToken)
        {
            var mode = force ? CharacterFlushMode.Shutdown : CharacterFlushMode.Periodic;
            var start = Stopwatch.GetTimestamp();
            var outcome = TickOutcome.Failure;
            try
            {
                if (await FlushCoreAsync(force, cancellationToken))
                {
                    outcome = TickOutcome.Success;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                outcome = TickOutcome.Cancelled;
                throw;
            }
            finally
            {
                GameWorldMetrics.RecordCharacterFlush(mode, outcome, Stopwatch.GetElapsedTime(start));
            }
        }

        private async Task<bool> FlushCoreAsync(bool force, CancellationToken cancellationToken)
        {
            await using (var recoveryScope = _serviceProvider.CreateAsyncScope())
            {
                var logoutService = recoveryScope.ServiceProvider.GetRequiredService<ICharacterLogoutService>();
                await logoutService.RecoverPendingLogoutsAsync(cancellationToken);
            }

            var snapshots = await CaptureSnapshotsAsync(cancellationToken);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            };
            var failures = new ConcurrentBag<Exception>();
            await Parallel.ForEachAsync(snapshots, options, async (entry, token) =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                try
                {
                    var persistenceService = scope.ServiceProvider.GetRequiredService<ICharacterPersistenceService>();
                    var logoutService = scope.ServiceProvider.GetRequiredService<ICharacterLogoutService>();
                    if (logoutService.IsPendingLogout(entry.Key))
                    {
                        return;
                    }

                    await persistenceService.PersistAsync(entry.Key, entry.Value, force, token);
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
                            entry.Key);
                    }
                }
            });

            if (force && !failures.IsEmpty)
            {
                throw new AggregateException("One or more character snapshots could not be durably handed off during shutdown.", failures);
            }

            return failures.IsEmpty;
        }

        private async Task<IReadOnlyDictionary<uint, CharacterModel>> CaptureSnapshotsAsync(CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<IReadOnlyDictionary<uint, CharacterModel>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _taskScheduler.Schedule(new RsAsyncTask(async token =>
            {
                var start = Stopwatch.GetTimestamp();
                var outcome = TickOutcome.Failure;
                try
                {
                    var characters = await _characterStore.GetSnapshotAsync(token);
                    using var scope = _serviceProvider.CreateScope();
                    var dehydrationService = scope.ServiceProvider.GetRequiredService<ICharacterDehydrationService>();
                    var persistenceState = scope.ServiceProvider.GetRequiredService<CharacterPersistenceState>();
                    var snapshots = new Dictionary<uint, CharacterModel>();
                    foreach (var character in characters.Values)
                    {
                        token.ThrowIfCancellationRequested();
                        var snapshot = dehydrationService.Dehydrate(character);
                        snapshots[character.MasterId] = snapshot with
                        {
                            SnapshotRevision = persistenceState.NextRevision(character.MasterId)
                        };
                    }

                    outcome = TickOutcome.Success;
                    completion.TrySetResult(snapshots);
                }
                catch (Exception exception)
                {
                    if (exception is OperationCanceledException && token.IsCancellationRequested)
                    {
                        outcome = TickOutcome.Cancelled;
                    }

                    completion.TrySetException(exception);
                    throw;
                }
                finally
                {
                    GameWorldMetrics.RecordCharacterSnapshot(outcome, Stopwatch.GetElapsedTime(start));
                }
            }, cancellationToken));

            return await completion.Task.WaitAsync(cancellationToken);
        }

    }
}
