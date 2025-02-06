using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Services
{
    public interface ICharacterService
    {
        public Task<Result<bool>> GetExistsAsync(uint masterId);
        public Task<Result<Appearance>> GetAppearanceAsync(uint masterId);
        public Task<Result<Statistics>> GetStatisticsAsync(uint masterId);
        public Task<Result<Details>> GetDetailsAsync(uint masterId);
        public Task<Result<IReadOnlyList<Item>>> GetItemsAsync(uint masterId);
        public Task<Result<Familiar>> GetFamiliarAsync(uint masterId);
        public Task<Result<Music>> GetMusicAsync(uint masterId);
        public Task<Result<Farming>> GetFarmingAsync(uint masterId);
        public Task<Result<Slayer>> GetSlayerAsync(uint masterId);
        public Task<Result<Notes>> GetNotesAsync(uint masterId);
        public Task<Result<TValue>> GetProfileDataByKeyAsync<TValue>(uint masterId, string key);
        public Task<Result<ProfileModel>> GetProfileAsync(uint masterId);
        public Task<Result<IReadOnlyList<ItemAppearance>>> GetItemAppearancesAsync(uint masterId);
        public Task<Result<State>> GetStateAsync(uint masterId);
    }
}
