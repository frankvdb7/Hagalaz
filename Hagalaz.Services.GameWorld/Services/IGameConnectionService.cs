using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Network.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface IGameConnectionService
    {
        public Task<IGameConnection?> FindById(string connectionId);
        public Task<IGameConnection?> FindByMasterId(uint masterId);
        public IAsyncEnumerable<IGameConnection> FindAll();
    }
}
