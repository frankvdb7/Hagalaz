using System;
using System.Linq;
using Hagalaz.Data;
using Hagalaz.Data.Entities;
using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameLogon.Data
{
    public class CharacterRepository : RepositoryBase<Character>, ICharacterRepository
    {
        public CharacterRepository(HagalazDbContext context) : base(context) { }

        public IQueryable<Character> FindByIdAsync(uint masterId) => FindAll().Where(c => c.Id == masterId);

        public IQueryable<Character> FindByDisplayNameAsync(string displayName) =>
            FindAll().Where(c => c.DisplayName.Equals(displayName, StringComparison.InvariantCultureIgnoreCase)).OrderBy(c => c.DisplayName).Take(1);
    }
}