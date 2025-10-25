using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Factories;
using Microsoft.Extensions.Logging;
using System;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Providers
{
    /// <summary>
    /// A provider for decoding map data from the cache.
    /// </summary>
    public class MapProvider : IMapProvider
    {
        private readonly ICacheAPI _cache;
        private readonly IMapCodec _codec;
        private readonly ITypeFactory<IMapType> _typeFactory;
        private readonly ILogger<MapProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapProvider"/> class.
        /// </summary>
        public MapProvider(ICacheAPI cache, IMapCodec codec, ITypeFactory<IMapType> typeFactory, ILogger<MapProvider> logger)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public int ArchiveSize => 0;

        /// <inheritdoc />
        public IMapType Get(int typeId) => Get(typeId, null);

        /// <inheritdoc />
        public IMapType Get(int typeId, int[]? xteaKeys = null)
        {
            var terrainData = ReadTerrainData(typeId);
            var objectData = ReadObjectData(typeId, xteaKeys);

            if (terrainData == null && objectData == null)
            {
                return _typeFactory.CreateType(typeId);
            }

            var combinedStream = new MemoryStream();
            var terrainBytes = terrainData?.ToArray() ?? Array.Empty<byte>();
            combinedStream.Write(BitConverter.GetBytes(terrainBytes.Length), 0, sizeof(int));
            combinedStream.Write(terrainBytes, 0, terrainBytes.Length);

            if (objectData != null)
            {
                objectData.WriteTo(combinedStream);
            }
            combinedStream.Position = 0;

            return _codec.Decode(typeId, combinedStream);
        }

        /// <inheritdoc />
        public IMapType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new IMapType[endTypeId - startTypeId];
            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                types[typeId - startTypeId] = Get(typeId);
            }
            return types;
        }

        /// <inheritdoc />
        public IMapType[] GetAll()
        {
            throw new NotSupportedException("Cannot get all maps at once.");
        }

        /// <inheritdoc />
        public void DecodePart(int regionID, int[] xteaKeys, int minX, int minY, int maxX, int maxY, int partZ, int partRotation, CalculateObjectPartRotation partRotationCallback, ObjectDecoded callback, ImpassibleTerrainDecoded groundCallback)
        {
            var terrainData = ReadTerrainData(regionID);
            var objectData = ReadObjectData(regionID, xteaKeys);

            sbyte[,,] dataArray = new sbyte[4, 64, 64];
            if (terrainData != null && terrainData.Length > 0)
            {
                using (var mapFormatReader = terrainData)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        for (int localX = 0; localX < 64; localX++)
                        {
                            for (int localY = 0; localY < 64; localY++)
                            {
                                while (true)
                                {
                                    int v = mapFormatReader.ReadUnsignedByte();
                                    if (v == 0) break;
                                    else if (v == 1) { mapFormatReader.ReadUnsignedByte(); break; }
                                    else if (v <= 49) { mapFormatReader.ReadSignedByte(); }
                                    else if (v <= 81) { dataArray[z, localX, localY] = (sbyte)(v - 49); }
                                }
                            }
                        }
                    }
                }
            }

            for (var z = 0; z < 4; z++)
            {
                for (var localX = 0; localX < 64; localX++)
                {
                    for (var localY = 0; localY < 64; localY++)
                    {
                        if ((dataArray[z, localX, localY] & 0x1) != 0)
                        {
                            int height = z;
                            if ((dataArray[1, localX, localY] & 0x2) != 0) height--;
                            if (height >= 0) groundCallback.Invoke(localX, localY, height);
                        }
                    }
                }
            }

            if (objectData != null && objectData.Length > 0)
            {
                using (var landscapeDataReader = objectData)
                {
                    var objectId = -1;
                    int incr;
                    while ((incr = landscapeDataReader.ReadHugeSmart()) != 0)
                    {
                        objectId += incr;
                        var location = 0;
                        int incr2;
                        while ((incr2 = landscapeDataReader.ReadSmart()) != 0)
                        {
                            location += incr2 - 1;
                            var localX = location >> 6 & 0x3f;
                            var localY = location & 0x3f;
                            var z = location >> 12;
                            var objectFlags = landscapeDataReader.ReadUnsignedByte();
                            var type = objectFlags >> 2;
                            var rotation = objectFlags & 0x3;
                            if (partZ == z && localX >= minX && localX <= maxX && localY >= minY && localY <= maxY)
                            {
                                int rotatedLocalX = minX + partRotationCallback.Invoke(objectId, rotation, localX & 0x7, localY & 0x7, partRotation, false);
                                int rotatedLocalY = minY + partRotationCallback.Invoke(objectId, rotation, localX & 0x7, localY & 0x7, partRotation, true);
                                int height = z;
                                if (rotatedLocalX < 0 || rotatedLocalX >= 64 || rotatedLocalY < 0 || rotatedLocalY >= 64) continue;
                                if ((dataArray[1, rotatedLocalX, rotatedLocalY] & 0x2) != 0) height--;
                                if (height >= 0) callback.Invoke(objectId, type, partRotation + rotation & 0x3, rotatedLocalX, rotatedLocalY, height);
                            }
                        }
                    }
                }
            }
        }

        private MemoryStream? ReadObjectData(int regionId, int[]? xteaKeys)
        {
            try
            {
                var fileId = _cache.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF));
                return _cache.ReadContainer(5, fileId, xteaKeys ?? Array.Empty<int>()).Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading object data for region {RegionId}", regionId);
                return null;
            }
        }

        private MemoryStream? ReadTerrainData(int regionId)
        {
            try
            {
                var fileId = _cache.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF));
                return _cache.ReadContainer(5, fileId).Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading terrain data for region {RegionId}", regionId);
                return null;
            }
        }
    }
}
