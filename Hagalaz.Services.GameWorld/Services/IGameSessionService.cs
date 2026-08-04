using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface IGameSessionService
    {
        public Task<IGameSession?> FindByMasterId(uint masterId);
        public Task<(IGameSession Session, bool Created)> AddSession(uint masterId, string connectionId);
        public Task<(IGameSession? Session, bool Created)> TryAddWorldSession(uint masterId, string connectionId, System.Threading.CancellationToken cancellationToken = default);
        public Task<bool> CommitWorldSession(IGameSession expectedSession, System.Threading.CancellationToken cancellationToken = default);
        public Task<bool> RemoveSession(IGameSession expectedSession, System.Threading.CancellationToken cancellationToken = default);
        public Task<bool> RemoveLocalSession(IGameSession expectedSession);
    }
}
