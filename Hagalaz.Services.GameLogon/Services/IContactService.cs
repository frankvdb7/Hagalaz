using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.GameLogon.Services.Model;

namespace Hagalaz.Services.GameLogon.Services
{
    public interface IContactService
    {
        ValueTask<IReadOnlyList<ContactDto>> FindFriendsByIdAsync(uint id);
        ValueTask<ContactDto?> FindFriendByIdAsync(uint id, uint contactId);
        ValueTask<IReadOnlyList<ContactDto>> FindIgnoresByIdAsync(uint id);
        ValueTask<ContactDto?> FindIgnoreByIdAsync(uint id, uint contactId);
        ValueTask<Result> AddContactAsync(uint id, uint contactId, bool ignore);
        ValueTask<Result> RemoveContactAsync(uint id, uint contactId);
        ValueTask<Result> SetContactSettingsAsync(uint id, ContactSettingsDto settings);
        ValueTask<ContactSettingsDto?> FindContactSettingsAsync(uint id);
    }
}