using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;
using System.Collections.Generic;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class MapCodec : IMapCodec
    {
        private const int MAP_PLANES = 4;
        private const int MAP_DIMENSION = 64;

        private const int TERRAIN_OPCODE_OFFSET = 49;
        private const int TERRAIN_OPCODE_TERMINATOR = 81;

        private const int LOCATION_Z_SHIFT = 12;
        private const int LOCATION_X_SHIFT = 6;
        private const int LOCATION_MASK = 0x3F;

        private const int OBJECT_SHAPE_SHIFT = 2;
        private const int OBJECT_ROTATION_MASK = 0x3;

        private const int TERRAIN_FLAG_BRIDGE = 0x2;

        public IMapType Decode(int id, MemoryStream stream)
        {
            var mapType = new MapType { Id = id };

            var terrainDataLength = stream.ReadInt();
            if (terrainDataLength > 0)
            {
                var terrainData = new byte[terrainDataLength];
                stream.Read(terrainData, 0, terrainDataLength);
                using (var terrainStream = new MemoryStream(terrainData))
                {
                    DecodeTerrainData(mapType.TerrainData, terrainStream);
                }
            }

            if (stream.Position < stream.Length)
            {
                DecodeObjectData(mapType.InternalObjects, mapType.TerrainData, stream);
            }
            return mapType;
        }

        public MemoryStream Encode(IMapType instance)
        {
            var stream = new MemoryStream();
            var terrainStream = new MemoryStream();

            for (var z = 0; z < MAP_PLANES; z++)
            for (var x = 0; x < MAP_DIMENSION; x++)
            for (var y = 0; y < MAP_DIMENSION; y++)
            {
                var value = instance.TerrainData[z, x, y];
                if (value != 0)
                {
                    terrainStream.WriteByte((byte)(value + TERRAIN_OPCODE_OFFSET));
                }
                terrainStream.WriteByte(0);
            }

            var terrainBytes = terrainStream.ToArray();
            stream.WriteInt(terrainBytes.Length);
            stream.Write(terrainBytes, 0, terrainBytes.Length);

            var groupedObjects = instance.Objects.GroupBy(o => o.Id).OrderBy(g => g.Key);
            var lastId = -1;
            foreach (var group in groupedObjects)
            {
                var id = group.Key;
                stream.WriteHugeSmart(id - lastId);
                lastId = id;

                var lastLocation = 0;
                foreach (var obj in group)
                {
                    var location = (obj.Z << LOCATION_Z_SHIFT) | (obj.X << LOCATION_X_SHIFT) | obj.Y;
                    stream.WriteSmart(location - lastLocation + 1);
                    lastLocation = location;
                    stream.WriteByte((byte)((obj.ShapeType << OBJECT_SHAPE_SHIFT) | obj.Rotation));
                }
                stream.WriteSmart(0);
            }
            stream.WriteHugeSmart(0);

            stream.Position = 0;
            return stream;
        }

        public static void DecodeTerrainData(sbyte[,,] terrainData, MemoryStream stream)
        {
            for (var z = 0; z < MAP_PLANES; z++)
            for (var x = 0; x < MAP_DIMENSION; x++)
            for (var y = 0; y < MAP_DIMENSION; y++)
            {
                while (true)
                {
                    var value = stream.ReadUnsignedByte();
                    if (value == 0) break;
                    if (value == 1) { stream.ReadUnsignedByte(); break; }
                    if (value <= TERRAIN_OPCODE_OFFSET) { stream.ReadSignedByte(); }
                    else if (value <= TERRAIN_OPCODE_TERMINATOR) { terrainData[z, x, y] = (sbyte)(value - TERRAIN_OPCODE_OFFSET); }
                }
            }
        }

        public static void DecodeObjectData(List<MapObject> objects, sbyte[,,] terrainData, MemoryStream stream)
        {
            var objectId = -1;
            int idOffset;
            while (stream.Position < stream.Length && (idOffset = stream.ReadHugeSmart()) != 0)
            {
                objectId += idOffset;
                var location = 0;
                int locationOffset;
                while (stream.Position < stream.Length && (locationOffset = stream.ReadSmart()) != 0)
                {
                    location += locationOffset - 1;
                    var x = location >> LOCATION_X_SHIFT & LOCATION_MASK;
                    var y = location & LOCATION_MASK;
                    var z = location >> LOCATION_Z_SHIFT;
                    var flags = stream.ReadUnsignedByte();
                    var type = flags >> OBJECT_SHAPE_SHIFT;
                    var rotation = flags & OBJECT_ROTATION_MASK;

                    if (x < 0 || x >= MAP_DIMENSION || y < 0 || y >= MAP_DIMENSION) continue;
                    if ((terrainData[1, x, y] & TERRAIN_FLAG_BRIDGE) != 0) z--;
                    if (z < 0 || z >= MAP_PLANES) continue;

                    objects.Add(new MapObject { Id = objectId, X = x, Y = y, Z = z, ShapeType = type, Rotation = rotation });
                }
            }
        }
    }
}
