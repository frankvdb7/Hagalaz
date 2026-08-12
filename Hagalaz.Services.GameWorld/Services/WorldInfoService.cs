using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            var worldInfos = await FindAllWorldInfoAsync(cancellationToken);
            var fingerprint = ComputeFingerprint(worldInfos);
            var cacheKey = GetCacheKey(fingerprint);

            return await _cache.GetOrCreateAsync(cacheKey,
                _ =>
                {
                    var locationInfos = MapLocationInfos(worldInfos);
                    return ValueTask.FromResult(new WorldInfoCacheDto(GetChecksum(fingerprint), locationInfos, worldInfos));
                },
                cancellationToken: cancellationToken);
        }

        private static string GetCacheKey(byte[] fingerprint) =>
            $"{Constants.Cache.WorldInfoCachePrefix}all:{Convert.ToHexString(fingerprint)}";

        private static int GetChecksum(byte[] fingerprint) =>
            BinaryPrimitives.ReadInt32BigEndian(fingerprint);

        private static byte[] ComputeFingerprint(IList<WorldInfo> worldInfos)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var info in worldInfos)
            {
                AppendInt32(hash, info.Id);
                AppendString(hash, info.Name);
                AppendString(hash, info.IpAddress);
                AppendInt32(hash, info.Port);
                AppendString(hash, info.Location.Name);
                AppendInt32(hash, info.Location.Flag);
                AppendBoolean(hash, info.Settings.IsMembersOnly);
                AppendBoolean(hash, info.Settings.IsQuickChatEnabled);
                AppendBoolean(hash, info.Settings.IsPvP);
                AppendBoolean(hash, info.Settings.IsLootShareEnabled);
                AppendBoolean(hash, info.Settings.IsHighLighted);
            }

            return hash.GetHashAndReset();
        }

        private static void AppendBoolean(IncrementalHash hash, bool value) => AppendInt32(hash, value ? 1 : 0);

        private static void AppendInt32(IncrementalHash hash, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            hash.AppendData(buffer);
        }

        private static void AppendString(IncrementalHash hash, string? value)
        {
            if (value == null)
            {
                AppendInt32(hash, -1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            AppendInt32(hash, bytes.Length);
            hash.AppendData(bytes);
        }

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
            }

            return Task.CompletedTask;
        }

        public Task UpdateWorldCharacterInfoAsync(WorldCharacterInfo worldCharacterInfo)
        {
            if (_worldInfoStore.TryGetValue(worldCharacterInfo.Id, out var wi))
            {
                _worldInfoStore[worldCharacterInfo.Id] = wi with
                {
                    CharacterCount = worldCharacterInfo.CharacterCount,
                    Online = worldCharacterInfo.Online
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

        private static bool MetadataEquals(Store.Model.WorldInfo left, Store.Model.WorldInfo right) =>
            left.Id == right.Id && left.Name == right.Name && left.IpAddress == right.IpAddress && left.Port == right.Port &&
            left.Location == right.Location && left.Settings == right.Settings;

    }
}
