using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Caching.Hybrid;

namespace Hagalaz.Services.GameWorld.Services
{
    public class WorldInfoService : IWorldInfoService
    {
        private const string _cacheKey = "world-info";
        private static int _checksum;
        private readonly WorldInfoStore _worldInfoStore;
        private readonly IMapper _mapper;
        private readonly HybridCache _cache;

        public WorldInfoService(WorldInfoStore worldInfoStore, IMapper mapper, HybridCache cache)
        {
            _worldInfoStore = worldInfoStore;
            _mapper = mapper;
            _cache = cache;
        }

        public async ValueTask<WorldInfoCacheDto> GetCacheAsync() =>
            await _cache.GetOrCreateAsync(_cacheKey,
                async _ =>
                {
                    var worldInfos = await FindAllWorldInfoAsync();
                    var locationInfos = GetLocationInfos(worldInfos);
                    var result = Interlocked.Increment(ref _checksum);
                    return new WorldInfoCacheDto(result,
                        locationInfos,
                        worldInfos);
                });

        private IList<WorldLocationInfo> GetLocationInfos(IList<WorldInfo> worldInfos) =>
            worldInfos
                .Select(info => info.Location)
                .DistinctBy(location => location.Flag)
                .ToList();

        public ValueTask<IList<WorldInfo>> FindAllWorldInfoAsync() => ValueTask.FromResult(_mapper.Map<IList<WorldInfo>>(_worldInfoStore.ToList()));

        public ValueTask<IList<WorldCharacterInfo>> FindAllWorldCharacterInfoAsync() =>
            ValueTask.FromResult(_mapper.Map<IList<WorldCharacterInfo>>(_worldInfoStore.ToList()));

        public Task AddOrUpdateWorldInfoAsync(WorldInfo worldInfo)
        {
            var info = _mapper.Map<Store.Model.WorldInfo>(worldInfo);
            var storeInfo = _worldInfoStore[worldInfo.Id];
            if (storeInfo == null)
            {
                _worldInfoStore[worldInfo.Id] = info;
            }
            else
            {
                _worldInfoStore[worldInfo.Id] = info with
                {
                    Online = storeInfo.Online
                };
            }

            return Task.CompletedTask;
        }

        public Task UpdateWorldCharacterInfoAsync(WorldCharacterInfo worldCharacterInfo)
        {
            if (_worldInfoStore.TryGetValue(worldCharacterInfo.Id, out var wi))
            {
                _worldInfoStore[worldCharacterInfo.Id] = wi with
                {
                    CharacterCount = worldCharacterInfo.CharacterCount, Online = worldCharacterInfo.Online
                };
            }

            return Task.CompletedTask;
        }

        public Task RemoveWorldInfoAsync(int id)
        {
            _worldInfoStore.TryRemove(id);
            return Task.CompletedTask;
        }

        public Task AddCharacter(WorldCharacter character)
        {
            if (_worldInfoStore.TryGetValue(character.WorldId, out var worldInfo))
            {
                worldInfo.CharacterCount++;
            }

            return Task.CompletedTask;
        }

        public Task RemoveCharacter(WorldCharacter character)
        {
            if (!_worldInfoStore.TryGetValue(character.WorldId, out var worldInfo))
            {
                return Task.CompletedTask;
            }

            var count = worldInfo.CharacterCount - 1;
            if (count >= 0)
            {
                worldInfo.CharacterCount = count;
            }

            return Task.CompletedTask;
        }
    }
}