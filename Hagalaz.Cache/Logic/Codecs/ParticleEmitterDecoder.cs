using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 Class210 emitter settings, index 27 archive 0.</summary>
public static class ParticleEmitterDecoder
{
    public sealed record Definition(int TextureId, uint ColorMin, uint ColorMax, float LifeMin, float LifeMax,
        float RateMin, float RateMax, float SizeMin, float SizeMax, float SpeedMin, float SpeedMax, int[] Angles, int[] UnsupportedOpcodes);

    public static Definition Decode(byte[] data)
    {
        var r = new Reader(data); var texture = -1;
        uint colorMin = 0, colorMax = 0;
        float lifeMin = 0, lifeMax = 0, rateMin = 0, rateMax = 0, sizeMin = 0, sizeMax = 0, speedMin = 0, speedMax = 0;
        var angles = new int[4]; var unsupported = new List<int>();
        while (true)
        {
            var op = r.Byte();
            if (op == 0) break;
            switch (op)
            {
                case 1: for (var i = 0; i < 4; i++) angles[i] = r.Short() * 8; break;
                case 3: speedMin = r.Int() / 262144f * 1000; speedMax = r.Int() / 262144f * 1000; break;
                case 5: sizeMin = sizeMax = r.Short() * 4; break;
                case 6: colorMin = unchecked((uint)r.Int()); colorMax = unchecked((uint)r.Int()); break;
                case 7: lifeMin = r.Short() / 1000f; lifeMax = r.Short() / 1000f; break;
                case 8: rateMin = r.Short() * 1000f / 64; rateMax = r.Short() * 1000f / 64; break;
                case 15: texture = r.Short(); if (texture == 65535) texture = -1; break;
                case 31: sizeMin = r.Short() * 4; sizeMax = r.Short() * 4; break;
                default:
                    unsupported.Add(op);
                    var length = op switch {
                        2 or 12 or 13 or 19 or 20 or 21 or 23 or 28 => 1,
                        4 or 14 or 17 or 27 or 29 => 2,
                        16 => 6, 18 or 22 => 4,
                        24 or 26 or 30 or 32 or 33 or 34 => 0,
                        9 or 10 or 25 => r.Byte() * 2,
                        _ => throw new NotSupportedException($"Unsupported particle emitter opcode {op}.")
                    };
                    r.Skip(length); break;
            }
        }
        return new(texture, colorMin, colorMax, lifeMin, lifeMax, rateMin, rateMax, sizeMin, sizeMax, speedMin, speedMax, angles, unsupported.ToArray());
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> data;
        private int offset;
        public Reader(ReadOnlySpan<byte> bytes) { data = bytes; offset = 0; }
        public byte Byte() { Check(1); return data[offset++]; }
        public int Short() { Check(2); var result = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]); offset += 2; return result; }
        public int Int() { Check(4); var result = BinaryPrimitives.ReadInt32BigEndian(data[offset..]); offset += 4; return result; }
        public void Skip(int count) { Check(count); offset += count; }
        private readonly void Check(int count) { if (offset > data.Length - count) throw new InvalidDataException("Truncated particle emitter definition."); }
    }
}
