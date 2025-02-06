using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Model;
using Hagalaz.Cache.Extensions;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Class MapsReader
    /// </summary>
    public class MapDecoder : IMapDecoder
    {
        private readonly ICacheAPI _cacheApi;
        private readonly ILogger<MapDecoder> _logger;

        /// <summary>
        /// Callback to calculate object part rotation.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="objectRotation">The object rotation.</param>
        /// <param name="xIndex">Index of the x.</param>
        /// <param name="yIndex">Index of the y.</param>
        /// <param name="partRotation">The part rotation.</param>
        /// <param name="calculateRotationY">if set to <c>true</c> [calculate rotation y].</param>
        /// <returns></returns>
        public delegate int CalculateObjectPartRotation(int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY);

        public MapDecoder(ICacheAPI cacheApi, ILogger<MapDecoder> logger)
        {
            _cacheApi = cacheApi;
            _logger = logger;
        }

        /// <summary>
        /// Reads region.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="regionId">Region id.</param>
        /// <param name="xteaKeys">xtea keys to decrypt that region.</param>
        /// <returns>Returns buffer or null if something failed.</returns>
        private MemoryStream? ReadObjectData(int regionId, int[] xteaKeys)
        {
            try
            {
                var fileId = _cacheApi.GetFileId(5, "l" + (regionId >> 8) + "_" + (regionId & 0xFF));
                return _cacheApi.ReadContainer(5, fileId, xteaKeys).Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading object data");
                return null;
            }
        }

        /// <summary>
        /// Reads region.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="regionId">Region id.</param>
        /// <returns>Returns buffer or null if something failed.</returns>
        private MemoryStream? ReadTerrainData(int regionId)
        {
            try
            {
                var fileId = _cacheApi.GetFileId(5, "m" + (regionId >> 8) + "_" + (regionId & 0xFF));
                return _cacheApi.ReadContainer(5, fileId).Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading terrain data");
                return null;
            }
        }

        /// <summary>
        /// Decodes all objects for given region.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="regionId">The regionid.</param>
        /// <param name="xteaKeys">The xtea keys.</param>
        /// <param name="objectDecoded">The callback.</param>
        /// <param name="impassableTerrainDecoded">The ground callback.</param>
        public void Decode(int regionId, int[] xteaKeys, ObjectDecoded objectDecoded, ImpassibleTerrainDecoded impassableTerrainDecoded)
        {
            var terrainData = ReadTerrainData(regionId);
            var objectData = ReadObjectData(regionId, xteaKeys);

            var decodedTerrainData = new sbyte[4, 64, 64];
            if (terrainData != null && terrainData.Length > 0)
            {
                using (var stream = terrainData)
                {
                    for (var z = 0; z < 4; z++)
                    {
                        for (var localX = 0; localX < 64; localX++)
                        {
                            for (var localY = 0; localY < 64; localY++)
                            {
                                while (true)
                                {
                                    var v = stream.ReadUnsignedByte();
                                    if (v == 0)
                                    {
                                        break;
                                    }
                                    else if (v == 1)
                                    {
                                        stream.ReadUnsignedByte();
                                        break;
                                    }
                                    else if (v <= 49)
                                    {
                                        stream.ReadSignedByte();
                                    }
                                    else if (v <= 81)
                                    {
                                        decodedTerrainData[z, localX, localY] = (sbyte)(v - 49);
                                    }
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
                        if ((decodedTerrainData[z, localX, localY] & 0x1) != 0)
                        {
                            var height = z;
                            if ((decodedTerrainData[1, localX, localY] & 0x2) != 0)
                            {
                                height--;
                            }

                            if (height >= 0)
                            {
                                impassableTerrainDecoded(localX, localY, height);
                            }
                        }
                    }
                }
            }

            if (objectData != null && objectData.Length > 0)
            {
                using (var stream = objectData)
                {
                    var objectId = -1;
                    int incr;
                    while ((incr = stream.ReadHugeSmart()) != 0)
                    {
                        objectId += incr;
                        var location = 0;
                        int incr2;
                        while ((incr2 = stream.ReadSmart()) != 0)
                        {
                            location += incr2 - 1;
                            var localX = location >> 6 & 0x3f;
                            var localY = location & 0x3f;
                            var z = location >> 12;
                            var objectFlags = stream.ReadUnsignedByte();
                            var shapeType = objectFlags >> 2;
                            var rotation = objectFlags & 0x3;

                            if (localX < 0 || localX >= 64 || localY < 0 || localY >= 64)
                            {
                                continue;
                            }

                            if ((decodedTerrainData[1, localX, localY] & 0x2) != 0)
                            {
                                z--;
                            }

                            if (z < 0 || z >= 4)
                            {
                                continue;
                            }

                            objectDecoded(objectId, shapeType, rotation, localX, localY, z);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads the part objects.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="regionID">The region identifier.</param>
        /// <param name="xteaKeys">The xtea keys.</param>
        /// <param name="minX">The minimum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <param name="partZ">The part z.</param>
        /// <param name="partRotation">The part rotation.</param>
        /// <param name="partRotationCallback">The part rotation callback.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="groundCallback">The ground callback.</param>
        public void ReadPartObjects(
            int regionID, int[] xteaKeys, int minX, int minY, int maxX, int maxY, int partZ, int partRotation, CalculateObjectPartRotation partRotationCallback,
            ObjectDecoded callback, ImpassibleTerrainDecoded groundCallback)
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
                                    if (v == 0)
                                    {
                                        break;
                                    }
                                    else if (v == 1)
                                    {
                                        mapFormatReader.ReadUnsignedByte();
                                        break;
                                    }
                                    else if (v <= 49)
                                    {
                                        mapFormatReader.ReadSignedByte();
                                    }
                                    else if (v <= 81)
                                    {
                                        dataArray[z, localX, localY] = (sbyte)(v - 49);
                                    }
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
                            if ((dataArray[1, localX, localY] & 0x2) != 0)
                            {
                                height--;
                            }

                            if (height >= 0)
                            {
                                groundCallback.Invoke(localX, localY, height);
                            }
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

                                if (rotatedLocalX < 0 || rotatedLocalX >= 64 || rotatedLocalY < 0 || rotatedLocalY >= 64)
                                {
                                    continue;
                                }

                                //if ((dataArray[1][localX][localY] & 2) == 2)
                                if ((dataArray[1, rotatedLocalX, rotatedLocalY] & 0x2) != 0)
                                {
                                    height--;
                                }

                                if (height >= 0)
                                {
                                    callback.Invoke(objectId, type, partRotation + rotation & 0x3, rotatedLocalX, rotatedLocalY, height);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}