using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface IGameSessionService
    {
        public Task<IGameSession?> FindByMasterId(uint masterId);
        public Task<(IGameSession Session, bool Created)> AddSession(uint masterId, string connectionId);
        public Task<bool> RemoveSession(string connectionId);
    }
}
