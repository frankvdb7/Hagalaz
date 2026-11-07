using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Render projection; gameplay flags and bridge-adjusted collision planes are unchanged.</summary>
public static class MapRenderDecoder
{
    public static TerrainRenderPlane[] DecodeTerrain(Stream stream, int regionId, CancellationToken cancellationToken = default)
    {
        if (regionId is < 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(regionId));
        var planes = Enumerable.Range(0, 4).Select(_ => new TerrainRenderPlane(new int[4096], new int[4096], new int[4096], new int[4096], new int[4096], new int[4096])).ToArray();
        for (var z = 0; z < 4; z++)
        for (var x = 0; x < 64; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var y = 0; y < 64; y++)
            {
                var tile = x * 64 + y;
                var plane = planes[z];
                while (true)
                {
                    var opcode = ReadByte(stream);
                    if (opcode <= 1)
                    {
                        var height = opcode == 1 ? ReadByte(stream) : 0;
                        if (height == 1) height = 0;
                        plane.Heights[tile] = opcode == 0
                            ? z == 0 ? TerrainHeight(((regionId >> 8) * 64) + x, ((regionId & 255) * 64) + y) : planes[z - 1].Heights[tile] - 960
                            : (z == 0 ? 0 : planes[z - 1].Heights[tile]) - height * 32;
                        break;
                    }
                    if (opcode <= 49)
                    {
                        plane.Overlays[tile] = ReadByte(stream);
                        plane.OverlayShapes[tile] = (opcode - 2) / 4;
                        plane.OverlayRotations[tile] = (opcode - 2) & 3;
                    }
                    else if (opcode <= 81) plane.Flags[tile] = opcode - 49;
                    else plane.Underlays[tile] = opcode - 81;
                }
            }
        }
        return planes;
    }

    public static MapAtmosphere? DecodeAtmosphere(Stream stream, CancellationToken cancellationToken = default)
    {
        MapAtmosphere? atmosphere = null;
        // Revision 742 Map.decode consumes these records after the last terrain tile.
        for (var opcode = stream.ReadByte(); opcode >= 0; opcode = stream.ReadByte())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (opcode)
            {
                case 0:
                    var flags = ReadByte(stream);
                    var rgb = (flags & 1) != 0 ? (ReadByte(stream) << 24 | ReadMedium(stream)) & 0xffffff : 0xffffff;
                    var intensity = (flags & 2) != 0 ? ReadShort(stream) / 256f : 1.1523438f;
                    var positive = (flags & 4) != 0 ? ReadShort(stream) / 256f : 0.69921875f;
                    var negative = (flags & 8) != 0 ? ReadShort(stream) / 256f : 1.2f;
                    var x = (flags & 16) != 0 ? (short)ReadShort(stream) : -50;
                    var y = (flags & 16) != 0 ? (short)ReadShort(stream) : -60;
                    var z = (flags & 16) != 0 ? (short)ReadShort(stream) : -50;
                    if ((flags & 32) != 0) SkipBytes(stream, 4); // Sky/fog RGB.
                    if ((flags & 64) != 0) SkipBytes(stream, 2); // Fog distance input.
                    if ((flags & 128) != 0) SkipBytes(stream, 12); // Environment-map faces.
                    atmosphere = new MapAtmosphere(rgb, intensity, positive, negative, x, y, z);
                    break;
                case 1:
                    var lights = ReadByte(stream);
                    for (var i = 0; i < lights; i++)
                    {
                        SkipBytes(stream, 7);
                        var radius = ReadByte(stream);
                        SkipBytes(stream, 2 * (radius * 2 + 1) + 2);
                        if ((ReadByte(stream) & 31) == 31) SkipBytes(stream, 2);
                    }
                    break;
                case 2:
                    SkipBytes(stream, 3);
                    break;
                case 128:
                    SkipBytes(stream, 10);
                    break;
                case 129:
                    for (var plane = 0; plane < 4; plane++)
                        if (ReadByte(stream) == 1) SkipBytes(stream, 256);
                    break;
                default:
                    return atmosphere;
            }
        }
        return atmosphere;
    }

    private static void SkipBytes(Stream stream, int count)
    {
        Span<byte> buffer = stackalloc byte[256];
        while (count > 0)
        {
            var length = Math.Min(count, buffer.Length);
            stream.ReadExactly(buffer[..length]);
            count -= length;
        }
    }

    public static MapRenderPlacement[] DecodeLandscape(Stream stream, CancellationToken cancellationToken = default)
    {
        var objects = new List<MapRenderPlacement>();
        var id = -1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var delta = 0;
            int part;
            do { part = ReadSmart(stream); delta = checked(delta + part); } while (part == 32767);
            if (delta == 0) return objects.ToArray();
            id = checked(id + delta);
            var location = 0;
            while (true)
            {
                var step = ReadSmart(stream);
                if (step == 0) break;
                location = checked(location + step - 1);
                var plane = location >> 12;
                if (plane > 3) throw new InvalidDataException("Landscape placement is outside the four source planes.");
                var flags = ReadByte(stream);
                objects.Add(new MapRenderPlacement(id, flags >> 2, flags & 3, (location >> 6) & 63, location & 63, plane));
            }
        }
    }

    public static FloorRenderDefinition DecodeFloor(Stream stream, int id, bool overlay)
    {
        int? rgb = 0, secondary = null;
        var texture = -1;
        var scale = 512;
        while (true)
        {
            var opcode = ReadByte(stream);
            if (opcode == 0) return new FloorRenderDefinition(id, overlay, rgb, texture, secondary, scale);
            if (opcode == 1) rgb = ReadMedium(stream);
            else if (overlay && opcode == 2) texture = ReadByte(stream);
            else if ((overlay && opcode == 3) || (!overlay && opcode == 2))
            {
                texture = ReadShort(stream);
                if (texture == 65535) texture = -1;
            }
            else if (overlay && opcode == 7) secondary = ReadMedium(stream);
            else if ((overlay && opcode == 9) || (!overlay && opcode == 3)) scale = ReadShort(stream) << 2;
            else if (overlay && opcode == 13) ReadMedium(stream);
            else if (overlay && (opcode == 20 || opcode == 22)) ReadShort(stream);
            else if (overlay && opcode is 11 or 14 or 16 or 21) ReadByte(stream);
            else if (overlay ? opcode is not (5 or 8 or 10 or 12) : opcode is not (4 or 5))
                throw new NotSupportedException($"Unsupported {(overlay ? "overlay" : "underlay")} opcode {opcode}.");
        }
    }

    public static int TerrainHeight(int x, int y)
    {
        x += 932731;
        y += 556238;
        var value = Noise(x + 45365, y + 91923, 4) - 128;
        value += (Noise(x + 10294, y + 37821, 2) - 128) >> 1;
        value += (Noise(x, y, 1) - 128) >> 2;
        return -Math.Clamp(35 + (int)(0.3 * value), 10, 60) * 32;
    }

    private static int Noise(int x, int y, int scale)
    {
        var cx = x / scale;
        var cy = y / scale;
        return Interpolate(Interpolate(Smooth(cx, cy), Smooth(cx + 1, cy), x & (scale - 1), scale),
            Interpolate(Smooth(cx, cy + 1), Smooth(cx + 1, cy + 1), x & (scale - 1), scale), y & (scale - 1), scale);
    }
    private static readonly int[] Cosines = Enumerable.Range(0, 16384).Select(i => (int)(16384.0 * Math.Cos(i * 0.0003834951969714103))).ToArray();
    private static int Interpolate(int a, int b, int offset, int scale)
    {
        var weight = (65536 - Cosines[offset * 8192 / scale]) >> 1;
        return (((65536 - weight) * a) >> 16) + ((weight * b) >> 16);
    }
    private static int Smooth(int x, int y) => (Raw(x - 1, y - 1) + Raw(x + 1, y - 1) + Raw(x - 1, y + 1) + Raw(x + 1, y + 1)) / 16
        + (Raw(x - 1, y) + Raw(x + 1, y) + Raw(x, y - 1) + Raw(x, y + 1)) / 8 + Raw(x, y) / 4;
    private static int Raw(int x, int y)
    {
        unchecked { var n = x + y * 57; n = (n << 13) ^ n; return ((1376312589 + (789221 + 15731 * n * n) * n) & int.MaxValue) >> 19 & 255; }
    }
    private static int ReadByte(Stream stream) { var value = stream.ReadByte(); return value < 0 ? throw new EndOfStreamException("Truncated map rendering data.") : value; }
    private static int ReadShort(Stream stream) => (ReadByte(stream) << 8) | ReadByte(stream);
    private static int ReadMedium(Stream stream) => (ReadByte(stream) << 16) | ReadShort(stream);
    private static int ReadSmart(Stream stream) { var first = ReadByte(stream); return first < 128 ? first : ((first << 8) | ReadByte(stream)) - 32768; }
}
