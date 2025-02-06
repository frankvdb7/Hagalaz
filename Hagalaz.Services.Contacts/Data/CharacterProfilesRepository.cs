using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.Contacts.Data
{
    public class CharacterProfilesRepository : RepositoryBase<CharactersProfile>, ICharacterProfilesRepository
    {
        public CharacterProfilesRepository(HagalazDbContext context) : base(context) { }

        public IQueryable<CharactersProfile> FindById(uint id) => FindAll().Where(p => p.MasterId == id);
    }
}