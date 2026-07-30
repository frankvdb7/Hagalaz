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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed class CharacterPersistenceService : ICharacterPersistenceService
    {
        private readonly ILogger<CharacterPersistenceService> _logger;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly CharacterPersistenceOutboxDbContext _outboxDbContext;
        private readonly ICharacterDehydrationService _dehydrationService;
        private readonly SnapshotRevisionGenerator _snapshotRevisionGenerator;
        private readonly CharacterPersistenceState _state;

        public CharacterPersistenceService(
            ILogger<CharacterPersistenceService> logger,
            IMapper mapper,
            IPublishEndpoint publishEndpoint,
            CharacterPersistenceOutboxDbContext outboxDbContext,
            ICharacterDehydrationService dehydrationService,
            SnapshotRevisionGenerator snapshotRevisionGenerator,
            CharacterPersistenceState state)
        {
            _logger = logger;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
            _outboxDbContext = outboxDbContext;
            _dehydrationService = dehydrationService;
            _snapshotRevisionGenerator = snapshotRevisionGenerator;
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
            var command = CreateCommand(_mapper, model, character.MasterId, snapshotRevision);

            // Publish through the scoped bus outbox and commit its row before updating the
            // in-memory fingerprint. A broker outage therefore leaves the command durable in
            // the database, while an outbox database failure is still visible to the caller.
            await _publishEndpoint.Publish(command, cancellationToken);
            await _outboxDbContext.SaveChangesAsync(cancellationToken);
            _state.MarkPersisted(character.MasterId, fingerprint);
            _logger.LogDebug("Queued character {MasterId} snapshot revision {SnapshotRevision} in the EF bus outbox", character.MasterId, snapshotRevision);
        }

        public void TrackPendingLogout(ICharacter character) => _state.TrackPendingLogout(character);

        public bool IsPendingLogout(ICharacter character) => _state.IsPendingLogout(character);

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

        private static string ComputeFingerprint(CharacterModel model) =>
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(model)));
    }

    public sealed class CharacterPersistenceState
    {
        private readonly ConcurrentDictionary<uint, string> _persistedFingerprints = new();
        private readonly ConcurrentDictionary<uint, ICharacter> _pendingLogouts = new();
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
            _pendingLogouts.TryRemove(masterId, out _);
        }

        public void TrackPendingLogout(ICharacter character) => _pendingLogouts[character.MasterId] = character;

        public bool IsPendingLogout(ICharacter character) =>
            _pendingLogouts.TryGetValue(character.MasterId, out var pendingCharacter) &&
            ReferenceEquals(pendingCharacter, character);

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
