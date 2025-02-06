using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.Contacts.Data
{
    public class CharacterContactsRepository : RepositoryBase<CharactersContact>, ICharacterContactsRepository
    {
        public CharacterContactsRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersContact> FindFriendsByMasterId(uint masterId) =>
            FindAll().Where(contact => contact.MasterId == masterId && contact.Type == 0);

        public IQueryable<CharactersContact> FindIgnoresByMasterId(uint masterId) =>
            FindAll().Where(contact => contact.MasterId == masterId && contact.Type == 1);

        public IQueryable<CharactersContact> FindContactByIdAsync(uint masterId, uint contactId) =>
            FindAll().Where(contact => contact.MasterId == masterId && contact.ContactId == contactId);

        public void AddContact(CharactersContact contact) => Add(contact);

        public void RemoveContact(CharactersContact contact) => Remove(contact);
    }
}