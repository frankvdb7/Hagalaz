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
        private static int _checksum;
        private static long _cacheVersion;
        private readonly WorldInfoStore _worldInfoStore;
        private readonly IMapper _mapper;
        private readonly HybridCache _cache;

        public WorldInfoService(WorldInfoStore worldInfoStore, IMapper mapper, HybridCache cache)
        {
            _worldInfoStore = worldInfoStore;
            _mapper = mapper;
            _cache = cache;
        }

        public async ValueTask<WorldInfoCacheDto> GetCacheAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = GetCacheKey();
            return await _cache.GetOrCreateAsync(cacheKey,
                async token =>
                {
                    var worldInfos = await FindAllWorldInfoAsync(token);
                    var locationInfos = MapLocationInfos(worldInfos);
                    var result = Interlocked.Increment(ref _checksum);
                    return new WorldInfoCacheDto(result, locationInfos, worldInfos);
                },
                cancellationToken: cancellationToken);
        }

        private static string GetCacheKey() =>
            $"{Constants.Cache.WorldInfoCachePrefix}all:{Interlocked.Read(ref _cacheVersion)}";

        private static List<WorldLocationInfo> MapLocationInfos(IList<WorldInfo> worldInfos) =>
            worldInfos
                .Select(info => info.Location)
                .OrderBy(location => location.Flag)
                .ThenBy(location => location.Name)
                .DistinctBy(location => location.Flag)
                .ToList();

        public ValueTask<IList<WorldInfo>> FindAllWorldInfoAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IList<WorldInfo>>(_mapper.Map<IList<WorldInfo>>(_worldInfoStore.ToList().OrderBy(info => info.Id)));

        public ValueTask<IList<WorldCharacterInfo>> FindAllWorldCharacterInfoAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IList<WorldCharacterInfo>>(_mapper.Map<IList<WorldCharacterInfo>>(_worldInfoStore.ToList().OrderBy(info => info.Id)));

        public Task AddOrUpdateWorldInfoAsync(WorldInfo worldInfo)
        {
            var info = _mapper.Map<Store.Model.WorldInfo>(worldInfo);
            var storeInfo = _worldInfoStore[worldInfo.Id];
            var updatedInfo = storeInfo == null
                ? info
                : info with
                {
                    Online = storeInfo.Online,
                    CharacterCount = storeInfo.CharacterCount
                };

            if (storeInfo == null || !MetadataEquals(storeInfo, updatedInfo))
            {
                _worldInfoStore[worldInfo.Id] = updatedInfo;
                InvalidateCache();
            }

            return Task.CompletedTask;
        }

        public Task UpdateWorldCharacterInfoAsync(WorldCharacterInfo worldCharacterInfo)
        {
            if (_worldInfoStore.TryGetValue(worldCharacterInfo.Id, out var wi))
            {
                var onlineChanged = wi.Online != worldCharacterInfo.Online;
                _worldInfoStore[worldCharacterInfo.Id] = wi with
                {
                    CharacterCount = worldCharacterInfo.CharacterCount,
                    Online = worldCharacterInfo.Online
                };
                if (onlineChanged)
                {
                    InvalidateCache();
                }
            }

            return Task.CompletedTask;
        }

        public Task RemoveWorldInfoAsync(int id)
        {
            if (_worldInfoStore.TryRemove(id))
            {
                InvalidateCache();
            }

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

        private static bool MetadataEquals(Store.Model.WorldInfo left, Store.Model.WorldInfo right) =>
            left.Id == right.Id && left.Name == right.Name && left.IpAddress == right.IpAddress && left.Port == right.Port &&
            left.Location == right.Location && left.Settings == right.Settings;

        private static void InvalidateCache() => Interlocked.Increment(ref _cacheVersion);
    }
}
