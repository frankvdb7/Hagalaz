using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameLogon.Data
{
    public class CharacterPermissionsRepository : RepositoryBase<CharactersPermission>, ICharacterPermissionsRepository
    {
        public CharacterPermissionsRepository(HagalazDbContext context) : base(context) { }

        public IQueryable<CharactersPermission> FindPermissionsByMasterIdAsync(uint masterId) => FindAll().Where(cp => cp.MasterId == masterId);
    }
}