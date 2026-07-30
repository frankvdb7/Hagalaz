using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed class CharacterPersistenceService : ICharacterPersistenceService
    {
        private const int MaxAttempts = 3;
        private readonly ILogger<CharacterPersistenceService> _logger;
        private readonly IMapper _mapper;
        private readonly IClientFactory _clientFactory;
        private readonly ICharacterDehydrationService _dehydrationService;
        private readonly SnapshotRevisionGenerator _snapshotRevisionGenerator;
        private readonly CharacterPersistenceOutbox _outbox;
        private readonly CharacterPersistenceState _state;

        public CharacterPersistenceService(
            ILogger<CharacterPersistenceService> logger,
            IMapper mapper,
            IClientFactory clientFactory,
            ICharacterDehydrationService dehydrationService,
            SnapshotRevisionGenerator snapshotRevisionGenerator,
            CharacterPersistenceOutbox outbox,
            CharacterPersistenceState state)
        {
            _logger = logger;
            _mapper = mapper;
            _clientFactory = clientFactory;
            _dehydrationService = dehydrationService;
            _snapshotRevisionGenerator = snapshotRevisionGenerator;
            _outbox = outbox;
            _state = state;
        }

        public async Task PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default)
        {
            using var characterLock = await _state.AcquireAsync(character.MasterId, cancellationToken);
            var model = await _dehydrationService.DehydrateAsync(character);
            var fingerprint = ComputeFingerprint(model);

            if (!force && _state.IsPersisted(character.MasterId, fingerprint))
            {
                return;
            }

            var snapshotRevision = _snapshotRevisionGenerator.Next();
            var request = CreateRequest(_mapper, model, character.MasterId, snapshotRevision);

            try
            {
                await SendWithRetriesAsync(request, cancellationToken);
                _state.MarkPersisted(character.MasterId, fingerprint);
                await _outbox.RemoveUpToAsync(character.MasterId, snapshotRevision, cancellationToken);
                _logger.LogDebug("Persisted character {MasterId} at snapshot revision {SnapshotRevision}", character.MasterId, snapshotRevision);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await EnqueueAfterFailureAsync(request, character.MasterId, snapshotRevision);
                throw;
            }
            catch (Exception exception)
            {
                await EnqueueAfterFailureAsync(request, character.MasterId, snapshotRevision, exception);
            }
        }

        public async Task ReplayPendingAsync(CancellationToken cancellationToken = default)
        {
            foreach (var pending in await _outbox.ReadAsync(cancellationToken))
            {
                try
                {
                    await SendWithRetriesAsync(pending.Request, cancellationToken);
                    await _outbox.RemoveUpToAsync(
                        pending.Request.MasterId,
                        pending.Request.SnapshotRevision,
                        cancellationToken);
                    _logger.LogInformation(
                        "Replayed character {MasterId} snapshot revision {SnapshotRevision} from the persistence outbox",
                        pending.Request.MasterId,
                        pending.Request.SnapshotRevision);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception,
                        "Could not replay character {MasterId} snapshot revision {SnapshotRevision} from the persistence outbox; it will remain queued",
                        pending.Request.MasterId,
                        pending.Request.SnapshotRevision);
                }
            }
        }

        private async Task SendWithRetriesAsync(UpdateCharacterRequest request, CancellationToken cancellationToken)
        {
            var requestClient = _clientFactory.CreateRequestClient<UpdateCharacterRequest>();
            Exception? lastException = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var response = await requestClient.GetResponse<UpdateCharacterResponse, CharacterNotFound>(request, cancellationToken);
                    if (response.Is<CharacterNotFound>(out _))
                    {
                        throw new InvalidOperationException($"Character '{request.MasterId}' was not found while persisting its snapshot.");
                    }

                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (attempt < MaxAttempts)
                {
                    lastException = exception;
                    var delay = TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(exception,
                        "Character {MasterId} persistence attempt {Attempt} of {MaxAttempts} failed; retrying in {DelayMilliseconds} ms",
                        request.MasterId, attempt, MaxAttempts, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw lastException ?? new InvalidOperationException($"Character '{request.MasterId}' persistence failed.");
        }

        private async Task EnqueueAfterFailureAsync(
            UpdateCharacterRequest request,
            uint masterId,
            long snapshotRevision,
            Exception? persistenceException = null)
        {
            try
            {
                // A shutdown cancellation token must not interrupt the durable handoff.
                await _outbox.EnqueueAsync(request, CancellationToken.None);
                _logger.LogWarning(persistenceException,
                    "Character {MasterId} snapshot revision {SnapshotRevision} was not sent to the character service and is durably queued for replay",
                    masterId,
                    snapshotRevision);
            }
            catch (Exception outboxException)
            {
                _logger.LogCritical(outboxException,
                    "Character {MasterId} snapshot revision {SnapshotRevision} could not be sent or durably queued",
                    masterId,
                    snapshotRevision);
                var exceptions = persistenceException is null
                    ? new[] { outboxException }
                    : new[] { persistenceException, outboxException };
                throw new AggregateException(exceptions);
            }
        }

        public void Forget(uint masterId) => _state.Forget(masterId);

        internal static UpdateCharacterRequest CreateRequest(IMapper mapper, CharacterModel model, uint masterId, long snapshotRevision) =>
            new(
                Guid.NewGuid(),
                masterId,
                mapper.Map<AppearanceDto>(model.Appearance),
                mapper.Map<DetailsDto>(model.Details),
                mapper.Map<StatisticsDto>(model.Statistics),
                mapper.Map<ItemCollectionDto>(model.ItemCollection),
                mapper.Map<FamiliarDto?>(model.Familiar),
                mapper.Map<MusicDto>(model.Music),
                mapper.Map<FarmingDto>(model.Farming),
                mapper.Map<SlayerDto>(model.Slayer),
                mapper.Map<NotesDto>(model.Notes),
                mapper.Map<ProfileDto>(model.Profile),
                mapper.Map<ItemAppearanceCollectionDto>(model.ItemAppearanceCollection),
                mapper.Map<StateDto>(model.State),
                snapshotRevision);

        private static string ComputeFingerprint(CharacterModel model) =>
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(model)));
    }

    public sealed class CharacterPersistenceState
    {
        private readonly ConcurrentDictionary<uint, string> _persistedFingerprints = new();
        private readonly Dictionary<uint, LockEntry> _locks = new();
        private readonly object _lockRegistryGate = new();

        public async Task<IDisposable> AcquireAsync(uint masterId, CancellationToken cancellationToken)
        {
            LockEntry entry;
            lock (_lockRegistryGate)
            {
                if (!_locks.TryGetValue(masterId, out entry!))
                {
                    entry = new LockEntry();
                    _locks.Add(masterId, entry);
                }

                entry.References++;
            }

            try
            {
                await entry.Semaphore.WaitAsync(cancellationToken);
                return new Releaser(this, masterId, entry);
            }
            catch
            {
                ReleaseReference(masterId, entry);
                throw;
            }
        }

        internal int LockCount
        {
            get
            {
                lock (_lockRegistryGate)
                {
                    return _locks.Count;
                }
            }
        }

        public bool IsPersisted(uint masterId, string fingerprint) =>
            _persistedFingerprints.TryGetValue(masterId, out var persistedFingerprint) && persistedFingerprint == fingerprint;

        public void MarkPersisted(uint masterId, string fingerprint) => _persistedFingerprints[masterId] = fingerprint;

        public void Forget(uint masterId)
        {
            _persistedFingerprints.TryRemove(masterId, out _);
        }

        private void Release(uint masterId, LockEntry entry)
        {
            entry.Semaphore.Release();
            ReleaseReference(masterId, entry);
        }

        private void ReleaseReference(uint masterId, LockEntry entry)
        {
            LockEntry? entryToDispose = null;
            lock (_lockRegistryGate)
            {
                entry.References--;
                if (entry.References == 0 && _locks.Remove(masterId))
                {
                    entryToDispose = entry;
                }
            }

            if (entryToDispose != null)
            {
                entryToDispose.Semaphore.Dispose();
            }
        }

        private sealed class LockEntry
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int References { get; set; }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly CharacterPersistenceState _owner;
            private readonly uint _masterId;
            private readonly LockEntry _entry;
            private int _released;

            public Releaser(CharacterPersistenceState owner, uint masterId, LockEntry entry)
            {
                _owner = owner;
                _masterId = masterId;
                _entry = entry;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    _owner.Release(_masterId, _entry);
                }
            }
        }
    }
}
