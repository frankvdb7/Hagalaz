using System;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
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
        private const int ROTATION_MASK = 0x3;
        private const int CHUNK_COORDINATE_MASK = 0x7;
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
            var terrainData = ReadTerrainData(typeId);
            var objectData = ReadObjectData(typeId, xteaKeys);

            if (terrainData == null && objectData == null)
            {
                return _typeFactory.CreateType(typeId);
            }

            var combinedStream = new MemoryStream();
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
        public void DecodePart(DecodePartRequest request)
        {
            var terrainDataStream = ReadTerrainData(request.RegionID);
            var objectDataStream = ReadObjectData(request.RegionID, request.XteaKeys);

            var terrainData = new sbyte[4, 64, 64];
            if (terrainDataStream != null && terrainDataStream.Length > 0)
            {
                MapCodec.DecodeTerrainData(terrainData, terrainDataStream);
            }

            for (var z = 0; z < 4; z++)
            {
                for (var localX = 0; localX < 64; localX++)
                {
                    for (var localY = 0; localY < 64; localY++)
                    {
                        if ((terrainData[z, localX, localY] & 0x1) != 0)
                        {
                            int height = z;
                            if ((terrainData[1, localX, localY] & BRIDGE_FLAG) != 0) height--;
                            if (height >= 0) request.GroundCallback.Invoke(localX, localY, height);
                        }
                    }
                }
            }

            if (objectDataStream != null && objectDataStream.Length > 0)
            {
                var objects = new System.Collections.Generic.List<MapObject>();
                MapCodec.DecodeObjectData(objects, terrainData, objectDataStream);

                foreach (var obj in objects)
                {
                    if (request.PartZ == obj.Z && obj.X >= request.MinX && obj.X <= request.MaxX && obj.Y >= request.MinY && obj.Y <= request.MaxY)
                    {
                        int rotatedLocalX = request.MinX + request.PartRotationCallback.Invoke(obj.Id, obj.Rotation, obj.X & CHUNK_COORDINATE_MASK, obj.Y & CHUNK_COORDINATE_MASK, request.PartRotation, false);
                        int rotatedLocalY = request.MinY + request.PartRotationCallback.Invoke(obj.Id, obj.Rotation, obj.X & CHUNK_COORDINATE_MASK, obj.Y & CHUNK_COORDINATE_MASK, request.PartRotation, true);
                        int height = obj.Z;
                        if (rotatedLocalX < 0 || rotatedLocalX >= 64 || rotatedLocalY < 0 || rotatedLocalY >= 64) continue;
                        if ((terrainData[1, rotatedLocalX, rotatedLocalY] & BRIDGE_FLAG) != 0) height--;
                        if (height >= 0) request.Callback.Invoke(obj.Id, obj.ShapeType, (request.PartRotation + obj.Rotation) & ROTATION_MASK, rotatedLocalX, rotatedLocalY, height);
                    }
                }
            }
        }
        private MemoryStream? ReadObjectData(int regionId, int[]? xteaKeys)
        {
            try
            {
                var fileId = _cache.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF));
                return _cache.ReadContainer(5, fileId, xteaKeys ?? []).Data;
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
