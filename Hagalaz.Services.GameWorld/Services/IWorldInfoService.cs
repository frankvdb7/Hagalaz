using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface IWorldInfoService
    {
        public ValueTask<IList<WorldInfo>> FindAllWorldInfoAsync();
        public ValueTask<IList<WorldCharacterInfo>> FindAllWorldCharacterInfoAsync();
        public Task AddCharacter(WorldCharacter character);
        public Task RemoveCharacter(WorldCharacter character);
        public Task AddOrUpdateWorldInfoAsync(WorldInfo worldInfo);
        public Task UpdateWorldCharacterInfoAsync(WorldCharacterInfo worldCharacterInfo);
        public Task RemoveWorldInfoAsync(int id);
        public ValueTask<WorldInfoCacheDto> GetCacheAsync();
    }
}
