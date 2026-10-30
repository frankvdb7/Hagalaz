using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Extracts animation sequence references from revision-742 config archive 32.</summary>
public static class RenderAnimationSetDecoder
{
    public static int[] Decode(MemoryStream stream)
    {
        var animations = new List<int>();
        while (stream.Position < stream.Length)
        {
            var opcode = stream.ReadByte();
            if (opcode == 0) return animations.Where(id => id >= 0).Distinct().ToArray();
            switch (opcode)
            {
                case 1: animations.Add(stream.ReadBigSmart()); animations.Add(stream.ReadBigSmart()); break;
                case >= 2 and <= 9:
                case >= 38 and <= 42:
                case >= 46 and <= 51: animations.Add(stream.ReadBigSmart()); break;
                case 26: case 54: Skip(2); break;
                case 27: Skip(13); break;
                case 28: var count = stream.ReadByte(); if (count < 0) throw new InvalidDataException("Truncated render animation set."); Skip(count); break;
                case 29: case 31: case 34: case 37: Skip(1); break;
                case 30: case 32: case 33: case 35: case 36: case 43: case 44: case 45: Skip(2); break;
                case 52:
                    var loops = stream.ReadByte();
                    if (loops < 0) throw new InvalidDataException("Truncated render animation set.");
                    for (var loop = 0; loop < loops; loop++) { animations.Add(stream.ReadBigSmart()); Skip(1); }
                    break;
                case 53: break;
                case 55: Skip(3); break;
                case 56: Skip(7); break;
                default: throw new NotSupportedException($"Render animation opcode {opcode} is not supported.");
            }
        }
        throw new InvalidDataException("Render animation set has no terminator.");

        void Skip(int length)
        {
            if (stream.Position > stream.Length - length) throw new InvalidDataException("Truncated render animation set.");
            stream.Position += length;
        }
    }
}
