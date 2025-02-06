using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterStateRepository : RepositoryBase<CharactersState>, ICharacterStateRepository
    {
        public CharacterStateRepository(HagalazDbContext context) : base(context)
        {
        }
    }
}
