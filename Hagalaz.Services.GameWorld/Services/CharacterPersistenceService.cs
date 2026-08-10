using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Data;
using Hagalaz.Characters.Messages.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed class CharacterPersistenceService : ICharacterPersistenceService
    {
        private readonly ILogger<CharacterPersistenceService> _logger;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HagalazDbContext _dbContext;
        private readonly ICharacterDehydrationService _dehydrationService;
        private readonly CharacterPersistenceState _state;

        public CharacterPersistenceService(
            ILogger<CharacterPersistenceService> logger,
            IMapper mapper,
            IPublishEndpoint publishEndpoint,
            HagalazDbContext dbContext,
            ICharacterDehydrationService dehydrationService,
            CharacterPersistenceState state)
        {
            _logger = logger;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
            _dbContext = dbContext;
            _dehydrationService = dehydrationService;
            _state = state;
        }

        public async Task PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default)
        {
            using var characterLock = await _state.AcquireAsync(character.MasterId, cancellationToken);
            var model = await _dehydrationService.DehydrateAsync(character);
            var command = CreateCommand(_mapper, model, character.MasterId, 0);
            var fingerprint = CharacterSnapshotFingerprint.Compute(command);

            if (!force && _state.IsPersisted(character.MasterId, fingerprint))
            {
                return;
            }

            var snapshotRevision = _state.NextRevision(character.MasterId);
            command = command with { SnapshotRevision = snapshotRevision };

            // Record the snapshot before publishing so a fast acknowledgement cannot arrive
            // before the producer has state to match it. If publishing or the outbox commit
            // fails, the pending snapshot remains eligible for redrive.
            _state.MarkPending(character.MasterId, command.CorrelationId, fingerprint, snapshotRevision);
            await _publishEndpoint.Publish(command, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Queued character {MasterId} snapshot revision {SnapshotRevision} in the EF bus outbox", character.MasterId, snapshotRevision);
        }

        public void TrackPendingLogout(ICharacter character) => _state.TrackPendingLogout(character);

        public void InitializeRevision(uint masterId, long persistedRevision) => _state.InitializeRevision(masterId, persistedRevision);

        public bool IsPendingLogout(ICharacter character) => _state.IsPendingLogout(character);

        public void MarkPendingLogoutRemoved(ICharacter character) => _state.MarkPendingLogoutRemoved(character);

        public bool IsPendingLogoutRemoved(ICharacter character) => _state.IsPendingLogoutRemoved(character);

        public IReadOnlyCollection<ICharacter> GetPendingLogouts() => _state.GetPendingLogouts();

        public bool IsPersistenceAcknowledged(ICharacter character) => _state.IsPersistenceAcknowledged(character.MasterId);

        public void Forget(uint masterId) => _state.Forget(masterId);

        internal static PersistCharacterCommand CreateCommand(IMapper mapper, CharacterModel model, uint masterId, long snapshotRevision) =>
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

    }

    public sealed class CharacterPersistenceState
    {
        private readonly ConcurrentDictionary<uint, string> _persistedFingerprints = new();
        private readonly ConcurrentDictionary<uint, PendingSnapshot> _pendingSnapshots = new();
        private readonly ConcurrentDictionary<uint, long> _nextRevisions = new();
        private readonly ConcurrentDictionary<uint, ICharacter> _pendingLogouts = new();
        private readonly ConcurrentDictionary<uint, byte> _removedPendingLogouts = new();
        private readonly ConcurrentDictionary<uint, byte> _completingLogouts = new();
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

        public void InitializeRevision(uint masterId, long persistedRevision)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(persistedRevision);
            _nextRevisions.AddOrUpdate(masterId, persistedRevision, (_, current) => Math.Max(current, persistedRevision));
        }

        public long NextRevision(uint masterId) =>
            _nextRevisions.AddOrUpdate(masterId, 1L, (_, current) => checked(current + 1));

        public void MarkPending(uint masterId, Guid correlationId, string fingerprint, long snapshotRevision) =>
            _pendingSnapshots[masterId] = new PendingSnapshot(correlationId, fingerprint, snapshotRevision);

        public void Acknowledge(uint masterId, Guid correlationId, long snapshotRevision)
        {
            if (!_pendingSnapshots.TryGetValue(masterId, out var pending) ||
                pending.CorrelationId != correlationId ||
                pending.SnapshotRevision != snapshotRevision)
            {
                return;
            }

            var pendingPair = new KeyValuePair<uint, PendingSnapshot>(masterId, pending);
            if (((ICollection<KeyValuePair<uint, PendingSnapshot>>)_pendingSnapshots).Remove(pendingPair))
            {
                _persistedFingerprints[masterId] = pending.Fingerprint;
            }
        }

        public void Forget(uint masterId)
        {
            _persistedFingerprints.TryRemove(masterId, out _);
            _pendingSnapshots.TryRemove(masterId, out _);
            _nextRevisions.TryRemove(masterId, out _);
            _pendingLogouts.TryRemove(masterId, out _);
            _removedPendingLogouts.TryRemove(masterId, out _);
        }

        public void TrackPendingLogout(ICharacter character)
        {
            _pendingLogouts[character.MasterId] = character;
            _removedPendingLogouts.TryRemove(character.MasterId, out _);
        }

        public bool IsPendingLogout(ICharacter character) =>
            _pendingLogouts.TryGetValue(character.MasterId, out var pendingCharacter) &&
            ReferenceEquals(pendingCharacter, character);

        public IReadOnlyCollection<ICharacter> GetPendingLogouts() => _pendingLogouts.Values.ToArray();

        public void MarkPendingLogoutRemoved(ICharacter character) => _removedPendingLogouts[character.MasterId] = 0;

        public bool IsPendingLogoutRemoved(ICharacter character) => _removedPendingLogouts.ContainsKey(character.MasterId);

        public bool IsPersistenceAcknowledged(uint masterId) => !_pendingSnapshots.ContainsKey(masterId);

        public bool TryGetPendingLogout(uint masterId, out ICharacter character) =>
            _pendingLogouts.TryGetValue(masterId, out character!);

        public bool TryBeginLogoutCompletion(uint masterId) =>
            _completingLogouts.TryAdd(masterId, 0);

        public void EndLogoutCompletion(uint masterId) => _completingLogouts.TryRemove(masterId, out _);

        private sealed record PendingSnapshot(Guid CorrelationId, string Fingerprint, long SnapshotRevision);

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
