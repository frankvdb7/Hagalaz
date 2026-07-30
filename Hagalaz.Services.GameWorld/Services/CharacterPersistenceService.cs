using System;
using System.Collections.Concurrent;
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
        private readonly CharacterPersistenceState _state;

        public CharacterPersistenceService(
            ILogger<CharacterPersistenceService> logger,
            IMapper mapper,
            IClientFactory clientFactory,
            ICharacterDehydrationService dehydrationService,
            SnapshotRevisionGenerator snapshotRevisionGenerator,
            CharacterPersistenceState state)
        {
            _logger = logger;
            _mapper = mapper;
            _clientFactory = clientFactory;
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
            var requestClient = _clientFactory.CreateRequestClient<UpdateCharacterRequest>();
            Exception? lastException = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    var request = CreateRequest(_mapper, model, character.MasterId, snapshotRevision);
                    var response = await requestClient.GetResponse<UpdateCharacterResponse, CharacterNotFound>(request, cancellationToken);
                    if (response.Is<CharacterNotFound>(out _))
                    {
                        throw new InvalidOperationException($"Character '{character.MasterId}' was not found while persisting its snapshot.");
                    }

                    _state.MarkPersisted(character.MasterId, fingerprint);
                    _logger.LogDebug("Persisted character {MasterId} at snapshot revision {SnapshotRevision}", character.MasterId, snapshotRevision);
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
                        character.MasterId, attempt, MaxAttempts, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            _logger.LogError(lastException,
                "Character {MasterId} snapshot revision {SnapshotRevision} could not be persisted after {MaxAttempts} attempts",
                character.MasterId, snapshotRevision, MaxAttempts);
            throw lastException ?? new InvalidOperationException($"Character '{character.MasterId}' persistence failed.");
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
        private readonly ConcurrentDictionary<uint, SemaphoreSlim> _locks = new();

        public Task<IDisposable> AcquireAsync(uint masterId, CancellationToken cancellationToken)
        {
            var semaphore = _locks.GetOrAdd(masterId, static _ => new SemaphoreSlim(1, 1));
            return AcquireCoreAsync(semaphore, cancellationToken);
        }

        public bool IsPersisted(uint masterId, string fingerprint) =>
            _persistedFingerprints.TryGetValue(masterId, out var persistedFingerprint) && persistedFingerprint == fingerprint;

        public void MarkPersisted(uint masterId, string fingerprint) => _persistedFingerprints[masterId] = fingerprint;

        public void Forget(uint masterId)
        {
            _persistedFingerprints.TryRemove(masterId, out _);
        }

        private static async Task<IDisposable> AcquireCoreAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new Releaser(semaphore);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

            public void Dispose() => _semaphore.Release();
        }
    }
}
