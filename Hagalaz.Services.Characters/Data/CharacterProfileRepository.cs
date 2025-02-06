using System.Linq;
using Hagalaz.Services.Common.Data;
using Microsoft.EntityFrameworkCore;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterProfileRepository : RepositoryBase<CharactersProfile>, ICharacterProfileRepository
    {
        private readonly HagalazDbContext _context;

        public CharacterProfileRepository(HagalazDbContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<CharactersProfile> FindProfileById(uint masterId) => FindAll().Where(p => p.MasterId == masterId);
        public IQueryable<string> FindProfileDataByKey(uint masterId, string key) => _context.Database
            .SqlQuery<string>($"SELECT JSON_UNQUOTE(JSON_EXTRACT(JSON_EXTRACT(Data, CONCAT('$.', {key})), '$')) AS Data FROM UserProfiles WHERE Id = {masterId}");
    }
}
