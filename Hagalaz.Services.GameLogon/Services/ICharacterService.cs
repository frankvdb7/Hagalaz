using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.GameLogon.Services.Model;
using Hagalaz.Services.GameLogon.Store.Model;

namespace Hagalaz.Services.GameLogon.Services
{
    public interface ICharacterService
    {
        ValueTask<CharacterSignInResult> SignInAsync(int worldId, string login, string password);
        ValueTask<Result> SignOutAsync(uint id);
        IAsyncEnumerable<CharacterDto> FindCharactersByWorldIdAsync(int id);
        ValueTask<CharacterDto?> FindCharacterBySessionIdAsync(uint id);
        ValueTask<CharacterDto?> FindCharacterByDisplayName(string displayName);
        ValueTask<CharacterSessionContext?> FindSessionByDisplayName(string displayName);
        ValueTask<CharacterSessionContext?> FindSessionByIdAsync(uint id);
    }
}