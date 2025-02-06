using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Contacts.Data
{
    public interface ICharacterContactsRepository
    {
        IQueryable<CharactersContact> FindFriendsByMasterId(uint masterId);
        IQueryable<CharactersContact> FindIgnoresByMasterId(uint masterId);
        IQueryable<CharactersContact> FindContactByIdAsync(uint masterId, uint contactId);
        void AddContact(CharactersContact contact);
        void RemoveContact(CharactersContact contact);
    }
}