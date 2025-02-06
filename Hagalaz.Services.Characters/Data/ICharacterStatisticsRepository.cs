using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterStatisticsRepository
    {
        IQueryable<CharactersStatistic> FindById(uint masterId);
        IQueryable<CharactersStatistic> FindAll();
        ValueTask<int> CountAsync();
    }
}
