using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface ICharacterPersistenceService
    {
        Task PersistAsync(ICharacter character, bool force, CancellationToken cancellationToken = default);
        void TrackPendingLogout(ICharacter character);
        bool IsPendingLogout(ICharacter character);
        IReadOnlyCollection<ICharacter> GetPendingLogouts();
        bool IsPersistenceAcknowledged(ICharacter character);
        void Acknowledge(uint masterId, long snapshotRevision);
        void Forget(uint masterId);
    }
}
