using System.Linq;
using Hagalaz.Services.Common.Data;
using Hagalaz.Data;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterMusicPlaylistRepository : RepositoryBase<CharactersMusicPlaylist>, ICharacterMusicPlaylistRepository
    {
        public CharacterMusicPlaylistRepository(HagalazDbContext context) : base(context)
        {
        }

        public IQueryable<CharactersMusicPlaylist> FindById(uint masterId) => FindAll().Where(e => e.MasterId == masterId);
    }
}
