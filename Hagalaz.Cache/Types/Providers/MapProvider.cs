using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Cache.Types.Providers
{
    public class MapProvider : IMapProvider
    {
        private const int BRIDGE_FLAG = 0x2;

        private readonly ICacheAPI _cache;
        private readonly IMapCodec _codec;
        private readonly ITypeFactory<IMapType> _typeFactory;
        private readonly ILogger<MapProvider> _logger;
        public MapProvider(ICacheAPI cache, IMapCodec codec, ITypeFactory<IMapType> typeFactory, ILogger<MapProvider> logger)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _logger = logger;
        }
        public int ArchiveSize => 0;
        public IMapType Get(int typeId) => Get(typeId, null);
        public IMapType Get(int typeId, int[]? xteaKeys = null)
        {
            using var terrainContainer = ReadTerrainContainer(typeId);
            using var objectContainer = ReadObjectContainer(typeId, xteaKeys);
            var terrainData = terrainContainer?.Data;
            var objectData = objectContainer?.Data;

            if (terrainData == null && objectData == null)
            {
                return _typeFactory.CreateType(typeId);
            }

            using var combinedStream = new MemoryStream();
            var terrainBytes = terrainData?.ToArray() ?? [];
            combinedStream.WriteInt(terrainBytes.Length);
            combinedStream.Write(terrainBytes, 0, terrainBytes.Length);

            if (objectData != null)
            {
                objectData.WriteTo(combinedStream);
            }
            combinedStream.Position = 0;

            return _codec.Decode(typeId, combinedStream);
        }
        public IMapType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new IMapType[endTypeId - startTypeId];
            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                types[typeId - startTypeId] = Get(typeId);
            }
            return types;
        }
        public IMapType[] GetAll() => throw new NotSupportedException("Cannot get all maps at once.");

        public DecodedMapRegion DecodeRegion(int regionId, int[] xteaKeys, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (terrainData, objects) = DecodeMapData(regionId, xteaKeys);
            var blockedTiles = DecodeGround(terrainData, cancellationToken);
            foreach (var _ in objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new DecodedMapRegion(objects, blockedTiles);
        }

        private (sbyte[,,] TerrainData, System.Collections.Generic.List<MapObject> Objects) DecodeMapData(int regionId, int[] xteaKeys)
        {
            using var terrainContainer = ReadTerrainContainer(regionId);
            using var objectContainer = ReadObjectContainer(regionId, xteaKeys);
            var terrainDataStream = terrainContainer?.Data;
            var objectDataStream = objectContainer?.Data;
            var terrainData = new sbyte[4, 64, 64];
            if (terrainDataStream != null && terrainDataStream.Length > 0)
            {
                MapCodec.DecodeTerrainData(terrainData, terrainDataStream);
            }

            var objects = new System.Collections.Generic.List<MapObject>();
            if (objectDataStream != null && objectDataStream.Length > 0)
            {
                MapCodec.DecodeObjectData(objects, terrainData, objectDataStream);
            }

            return (terrainData, objects);
        }

        private static List<BlockedTerrainTile> DecodeGround(sbyte[,,] terrainData, CancellationToken cancellationToken)
        {
            return new List<BlockedTerrainTile>(EnumerateBlockedTiles(terrainData, cancellationToken));
        }

        private static IEnumerable<BlockedTerrainTile> EnumerateBlockedTiles(sbyte[,,] terrainData, CancellationToken cancellationToken)
        {
            for (var z = 0; z < 4; z++)
            {
                for (var localX = 0; localX < 64; localX++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var localY = 0; localY < 64; localY++)
                    {
                        if ((terrainData[z, localX, localY] & 0x1) != 0)
                        {
                            int height = z;
                            if ((terrainData[1, localX, localY] & BRIDGE_FLAG) != 0) height--;
                            if (height >= 0) yield return new BlockedTerrainTile(localX, localY, height);
                        }
                    }
                }
            }
        }

        private IContainer? ReadObjectContainer(int regionId, int[]? xteaKeys)
        {
            var fileId = _cache.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF));
            if (fileId == -1)
            {
                _logger.LogDebug("Object data for region {RegionId} is absent", regionId);
                return null;
            }

            return xteaKeys is null || xteaKeys.Length == 0
                ? _cache.ReadContainer(5, fileId)
                : _cache.ReadContainer(5, fileId, xteaKeys);
        }
        private IContainer? ReadTerrainContainer(int regionId)
        {
            var fileId = _cache.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF));
            if (fileId == -1)
            {
                _logger.LogDebug("Terrain data for region {RegionId} is absent", regionId);
                return null;
            }

            return _cache.ReadContainer(5, fileId);
        }
    }
}
