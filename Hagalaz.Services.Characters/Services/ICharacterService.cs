using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using Hagalaz.Services.Characters.Services.Model;

namespace Hagalaz.Services.Characters.Services
{
    public interface ICharacterService
    {
        public Task<Result<bool>> GetExistsAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<long>> GetSnapshotRevisionAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Appearance>> GetAppearanceAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Statistics>> GetStatisticsAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Details>> GetDetailsAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<IReadOnlyList<Item>>> GetItemsAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Familiar>> GetFamiliarAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Music>> GetMusicAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Farming>> GetFarmingAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Slayer>> GetSlayerAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<Notes>> GetNotesAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<TValue>> GetProfileDataByKeyAsync<TValue>(uint masterId, string key, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<ProfileModel>> GetProfileAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<IReadOnlyList<ItemAppearance>>> GetItemAppearancesAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
        public Task<Result<State>> GetStateAsync(uint masterId, System.Threading.CancellationToken cancellationToken = default);
    }
}
