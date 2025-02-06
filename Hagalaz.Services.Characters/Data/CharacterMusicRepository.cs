using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterMusicRepository : RepositoryBase<CharactersMusic>, ICharacterMusicRepository
    {
        public CharacterMusicRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersMusic> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
