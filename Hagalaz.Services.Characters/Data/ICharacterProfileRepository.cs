using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterProfileRepository
    {
        public IQueryable<string> FindProfileDataByKey(uint masterId, string key);
        public IQueryable<CharactersProfile> FindProfileById(uint masterId);
    }
}
