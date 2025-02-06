using Hagalaz.Data.Entities;
using System.Linq;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterMusicPlaylistRepository
    {
        IQueryable<CharactersMusicPlaylist> FindById(uint masterId);
    }
}
