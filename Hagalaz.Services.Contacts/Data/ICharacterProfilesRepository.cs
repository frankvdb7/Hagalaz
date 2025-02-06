using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Contacts.Data
{
    public interface ICharacterProfilesRepository
    {
        public IQueryable<CharactersProfile> FindById(uint id);
    }
}