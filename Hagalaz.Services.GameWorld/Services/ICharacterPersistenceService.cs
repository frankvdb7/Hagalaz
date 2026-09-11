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

        public CharacterPersistenceReceipt(uint masterId, Guid correlationId, long snapshotRevision)
        {
            MasterId = masterId;
            CorrelationId = correlationId;
            SnapshotRevision = snapshotRevision;
        }

        public uint MasterId { get; }
        public Guid CorrelationId { get; }
        public long SnapshotRevision { get; }
        internal bool IsCompleted => _completion.Task.IsCompleted;
        internal bool TryAcknowledge(CharacterPersistenceOutcome outcome) => _completion.TrySetResult(outcome);

        internal Task<CharacterPersistenceOutcome> WaitAsync(CancellationToken cancellationToken) =>
            _completion.Task.WaitAsync(cancellationToken);
    }

    public interface ICharacterPersistenceService
    {
        Task<CharacterPersistenceReceipt?> PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default);
        Task<CharacterPersistenceOutcome> WaitForAcknowledgementAsync(CharacterPersistenceReceipt receipt, CancellationToken cancellationToken = default);
        void Acknowledge(uint masterId, Guid correlationId, long snapshotRevision, CharacterPersistenceOutcome outcome);
        void InitializeRevision(uint masterId, long persistedRevision);
    }
}
