using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Services.Common.Data;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterStatisticsRepository : RepositoryBase<CharactersStatistic>, ICharacterStatisticsRepository
    {
        public CharacterStatisticsRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersStatistic> FindById(uint masterId) => base.FindAll().Where(e => e.MasterId == masterId);
        public new IQueryable<CharactersStatistic> FindAll() => base.FindAll();
        public async ValueTask<int> CountAsync() => await base.FindAll().CountAsync();
    }
}
