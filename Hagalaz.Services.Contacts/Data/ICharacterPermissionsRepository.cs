using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Contacts.Data
{
    public interface ICharacterPermissionsRepository
    {
        public IQueryable<CharactersPermission> FindPermissionsByMasterIdAsync(uint id);
    }
}