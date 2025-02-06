using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterContactsRepository
    {
        IQueryable<CharactersContact> FindFriendsByMasterId(uint masterId);
        IQueryable<CharactersContact> FindIgnoresByMasterId(uint masterId);
        IQueryable<CharactersContact> FindContactByIdAsync(uint masterId, uint contactId);
        ValueTask AddContactAsync(CharactersContact contact);
        ValueTask RemoveContactAsync(CharactersContact contact);
    }
}