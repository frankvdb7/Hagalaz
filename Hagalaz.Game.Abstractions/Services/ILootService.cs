using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface ILootService
    {
        public Task<ILootTable?> FindNpcLootTable(int id);
        public Task<ILootTable?> FindItemLootTable(int id);
        public Task<ILootTable?> FindGameObjectLootTable(int id);
    }
}
