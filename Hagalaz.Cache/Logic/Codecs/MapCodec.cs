using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class MapCodec : IMapCodec
    {
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
                    for (var z = 0; z < 4; z++)
                    for (var x = 0; x < 64; x++)
                    for (var y = 0; y < 64; y++)
                    {
                        while (true)
                        {
                            var value = terrainStream.ReadUnsignedByte();
                            if (value == 0) break;
                            if (value == 1) { terrainStream.ReadUnsignedByte(); break; }
                            if (value <= 49) { terrainStream.ReadSignedByte(); }
                            else if (value <= 81) { mapType.TerrainData[z, x, y] = (sbyte)(value - 49); }
                        }
                    }
                }
            }

            if (stream.Position < stream.Length)
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
                        var x = location >> 6 & 0x3F;
                        var y = location & 0x3F;
                        var z = location >> 12;
                        var flags = stream.ReadUnsignedByte();
                        var type = flags >> 2;
                        var rotation = flags & 0x3;

                        if (x < 0 || x >= 64 || y < 0 || y >= 64) continue;
                        if ((mapType.TerrainData[1, x, y] & 0x2) != 0) z--;
                        if (z < 0 || z >= 4) continue;

                        mapType.InternalObjects.Add(new MapObject { Id = objectId, X = x, Y = y, Z = z, ShapeType = type, Rotation = rotation });
                    }
                }
            }
            return mapType;
        }

        public MemoryStream Encode(IMapType instance)
        {
            var stream = new MemoryStream();
            var terrainStream = new MemoryStream();

            for (var z = 0; z < 4; z++)
            for (var x = 0; x < 64; x++)
            for (var y = 0; y < 64; y++)
            {
                var value = instance.TerrainData[z, x, y];
                if (value != 0)
                {
                    terrainStream.WriteByte((byte)(value + 49));
                }
                terrainStream.WriteByte(0);
            }

            var terrainBytes = terrainStream.ToArray();
            stream.Write(BitConverter.GetBytes(terrainBytes.Length), 0, sizeof(int));
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
                    var location = (obj.Z << 12) | (obj.X << 6) | obj.Y;
                    stream.WriteSmart(location - lastLocation + 1);
                    lastLocation = location;
                    stream.WriteByte((byte)((obj.ShapeType << 2) | obj.Rotation));
                }
                stream.WriteSmart(0);
            }
            stream.WriteHugeSmart(0);

            stream.Position = 0;
            return stream;
        }
    }
}
