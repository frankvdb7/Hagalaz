using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterPermissionsRepository
    {
        public IQueryable<CharactersPermission> FindPermissionsByMasterIdAsync(uint id);
    }
}