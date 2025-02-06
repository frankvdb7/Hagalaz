using System;
using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameLogon.Data
{
    public class CharacterOffenceRepository : RepositoryBase<CharactersOffence>, ICharacterOffenceRepository
    {
        public CharacterOffenceRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersOffence> FindActiveOffencesByMasterIdAsync(uint masterId) =>
            FindAll().Where(offence => offence.MasterId == masterId && offence.ExpireDate > DateTime.Now);
    }
}