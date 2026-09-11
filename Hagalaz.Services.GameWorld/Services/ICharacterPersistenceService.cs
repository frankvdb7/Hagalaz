using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Services
{
    public sealed class CharacterPersistenceReceipt
    {
        private readonly TaskCompletionSource<CharacterPersistenceOutcome> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CharacterPersistenceReceipt(uint masterId, Guid correlationId, long snapshotRevision, Guid ownerId)
        {
            MasterId = masterId;
            CorrelationId = correlationId;
            SnapshotRevision = snapshotRevision;
            OwnerId = ownerId;
        }

        public uint MasterId { get; }
        public Guid CorrelationId { get; }
        public long SnapshotRevision { get; }
        public Guid OwnerId { get; }

        internal bool IsAcknowledgedSuccessfully =>
            _completion.Task.IsCompletedSuccessfully &&
            _completion.Task.Result is CharacterPersistenceOutcome.Committed or CharacterPersistenceOutcome.Duplicate;

        internal bool TryAcknowledge(CharacterPersistenceOutcome outcome) => _completion.TrySetResult(outcome);

        internal Task<CharacterPersistenceOutcome> WaitAsync(CancellationToken cancellationToken) =>
            _completion.Task.WaitAsync(cancellationToken);
    }

    public interface ICharacterPersistenceService
    {
        Task<CharacterPersistenceReceipt?> PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default);
        Task<CharacterPersistenceOutcome> WaitForAcknowledgementAsync(CharacterPersistenceReceipt receipt, CancellationToken cancellationToken = default);
        void InitializeRevision(uint masterId, long persistedRevision);
        bool IsPersistenceAcknowledged(ICharacter character);
        void Forget(CharacterPersistenceReceipt receipt);
    }
}
