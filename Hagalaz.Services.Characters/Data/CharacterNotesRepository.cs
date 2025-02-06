using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterNotesRepository : RepositoryBase<CharactersNote>, ICharacterNotesRepository
    {
        public CharacterNotesRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersNote> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
