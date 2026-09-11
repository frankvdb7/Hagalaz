using System;
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

        public async Task<CharacterPersistenceReceipt?> PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default)
        {
            using var characterLock = await _state.AcquireAsync(character.MasterId, cancellationToken);
            while (true)
            {
                if (_state.TryGetPending(character.MasterId, out var pending))
                {
                    if (!pending.IsCompleted)
                    {
                        if (!force)
                        {
                            return null;
                        }

                        await pending.WaitAsync(cancellationToken);
                    }

                    _state.RemovePending(character.MasterId, pending);
                    continue;
                }

                var model = await _dehydrationService.DehydrateAsync(character);
                var command = CreateCommand(_mapper, model, character.MasterId, 0);
                var fingerprint = CharacterSnapshotFingerprint.Compute(command);

                if (!force && _state.IsPersisted(character.MasterId, fingerprint))
                {
                    return null;
                }

                var snapshotRevision = _state.NextRevision(character.MasterId);
                command = command with { SnapshotRevision = snapshotRevision };
                var receipt = new CharacterPersistenceReceipt(
                    character.MasterId,
                    command.CorrelationId,
                    snapshotRevision);

                // Record the snapshot before publishing so a fast acknowledgement cannot arrive
                // before the producer has state to match it.
                _state.MarkPending(character.MasterId, fingerprint, receipt);

                try
                {
                    await _publishEndpoint.Publish(command, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    _state.RemovePending(character.MasterId, receipt);
                    throw;
                }

                _logger.LogDebug("Queued character {MasterId} snapshot revision {SnapshotRevision} in the EF bus outbox", character.MasterId, snapshotRevision);
                return receipt;
            }
        }

        public void InitializeRevision(uint masterId, long persistedRevision) => _state.InitializeRevision(masterId, persistedRevision);

        public Task<CharacterPersistenceOutcome> WaitForAcknowledgementAsync(
            CharacterPersistenceReceipt receipt,
            CancellationToken cancellationToken = default) => receipt.WaitAsync(cancellationToken);

        public void Acknowledge(
            uint masterId,
            Guid correlationId,
            long snapshotRevision,
            CharacterPersistenceOutcome outcome) =>
            _state.Acknowledge(masterId, correlationId, snapshotRevision, outcome);

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
        private readonly Dictionary<uint, PersistenceEntry> _entries = new();
        private readonly object _stateGate = new();
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

        public bool IsPersisted(uint masterId, string fingerprint)
        {
            lock (_stateGate)
            {
                return _entries.TryGetValue(masterId, out var entry) && entry.PersistedFingerprint == fingerprint;
            }
        }

        public void InitializeRevision(uint masterId, long persistedRevision)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(persistedRevision);
            lock (_stateGate)
            {
                var entry = GetOrCreateEntry(masterId);
                entry.Revision = Math.Max(entry.Revision, persistedRevision);
            }
        }

        public long NextRevision(uint masterId)
        {
            lock (_stateGate)
            {
                var entry = GetOrCreateEntry(masterId);
                return entry.Revision = checked(entry.Revision + 1);
            }
        }

        public void MarkPending(uint masterId, string fingerprint, CharacterPersistenceReceipt receipt)
        {
            lock (_stateGate)
            {
                var entry = GetOrCreateEntry(masterId);
                if (entry.Pending is not null)
                {
                    throw new InvalidOperationException($"Character '{masterId}' already has an unacknowledged persistence operation.");
                }

                entry.Pending = new PendingSnapshot(fingerprint, receipt);
            }
        }

        public bool TryGetPending(uint masterId, out CharacterPersistenceReceipt receipt)
        {
            lock (_stateGate)
            {
                if (_entries.TryGetValue(masterId, out var entry) && entry.Pending is { } pending)
                {
                    receipt = pending.Receipt;
                    return true;
                }

                receipt = null!;
                return false;
            }
        }

        public void RemovePending(uint masterId, CharacterPersistenceReceipt receipt)
        {
            lock (_stateGate)
            {
                if (_entries.TryGetValue(masterId, out var entry) &&
                    entry.Pending is { } pending &&
                    ReferenceEquals(pending.Receipt, receipt))
                {
                    entry.Pending = null;
                }
            }
        }

        public void Acknowledge(uint masterId, Guid correlationId, long snapshotRevision, CharacterPersistenceOutcome outcome)
        {
            lock (_stateGate)
            {
                if (!_entries.TryGetValue(masterId, out var entry) ||
                    entry.Pending is not { } pending ||
                    pending.Receipt.CorrelationId != correlationId ||
                    pending.Receipt.SnapshotRevision != snapshotRevision)
                {
                    return;
                }

                if (!pending.Receipt.TryAcknowledge(outcome))
                {
                    return;
                }

                entry.Pending = null;
                if (outcome is CharacterPersistenceOutcome.Committed or CharacterPersistenceOutcome.Duplicate)
                {
                    entry.PersistedFingerprint = pending.Fingerprint;
                }
            }
        }

        private PersistenceEntry GetOrCreateEntry(uint masterId)
        {
            if (!_entries.TryGetValue(masterId, out var entry))
            {
                entry = new PersistenceEntry();
                _entries.Add(masterId, entry);
            }

            return entry;
        }

        private sealed class PersistenceEntry
        {
            public long Revision { get; set; }
            public string? PersistedFingerprint { get; set; }
            public PendingSnapshot? Pending { get; set; }
        }

        private sealed record PendingSnapshot(string Fingerprint, CharacterPersistenceReceipt Receipt);

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
