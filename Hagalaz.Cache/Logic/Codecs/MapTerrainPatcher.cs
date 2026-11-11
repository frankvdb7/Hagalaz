using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Rewrites individual tiles inside stored terrain. Decoded values cannot be re-encoded because they no longer
/// say which heights were automatic, so every byte outside the changed tokens is copied as stored.
/// </summary>
public static class MapTerrainPatcher
{
    private const int Planes = 4;
    private const int Dimension = 64;
    private const int ExplicitHeight = 1;
    private const int LastOverlayOpcode = 49;
    private const int LastFlagsOpcode = 81;

    /// <summary>One opcode carries the whole flag value, so only 0 (no token) to 32 can be stored.</summary>
    public static bool IsStorableFlags(int flags) => flags is >= 0 and <= LastFlagsOpcode - LastOverlayOpcode;

    public static byte[] Patch(byte[] stored, IEnumerable<MapTerrainTileChange> changes)
    {
        var byTile = new Dictionary<int, MapTerrainTileChange>();
        foreach (var change in changes)
        {
            if (change.Flags is { } flags && !IsStorableFlags(flags))
                throw new ArgumentOutOfRangeException(nameof(changes), flags, "A tile flag value must be between 0 and 32.");
            byTile[TileKey(change.Plane, change.X, change.Y)] = change;
        }

        using var output = new MemoryStream(stored.Length + byTile.Count * 4);
        var offset = 0;
        for (var z = 0; z < Planes; z++)
        for (var x = 0; x < Dimension; x++)
        for (var y = 0; y < Dimension; y++)
        {
            var end = TileEnd(stored, offset);
            if (byTile.TryGetValue(TileKey(z, x, y), out var change)) WriteTile(output, stored, offset, end, change);
            else output.Write(stored, offset, end - offset);
            offset = end;
        }
        output.Write(stored, offset, stored.Length - offset);
        return output.ToArray();
    }

    /// <summary>The offset of the first byte after the last terrain tile.</summary>
    public static int TerrainEnd(byte[] stored)
    {
        var offset = 0;
        for (var tile = 0; tile < Planes * Dimension * Dimension; tile++) offset = TileEnd(stored, offset);
        return offset;
    }

    private static int TileKey(int plane, int x, int y) => (plane << 12) | (x << 6) | y;

    private static int TileEnd(byte[] stored, int offset)
    {
        while (true)
        {
            var opcode = Opcode(stored, offset);
            offset += TokenLength(opcode);
            if (offset > stored.Length) throw new EndOfStreamException("Terrain ends inside a tile.");
            if (opcode <= ExplicitHeight) return offset;
        }
    }

    private static byte Opcode(byte[] stored, int offset) =>
        offset < stored.Length ? stored[offset] : throw new EndOfStreamException("Terrain ends before its last tile.");

    private static int TokenLength(int opcode) => opcode is >= ExplicitHeight and <= LastOverlayOpcode ? 2 : 1;

    private static void WriteTile(MemoryStream output, byte[] stored, int start, int end, MapTerrainTileChange change)
    {
        // An equal value keeps its stored tokens, so a save that changes nothing changes no bytes.
        var flags = change.Flags is { } submitted && submitted != StoredFlags(stored, start) ? submitted : (int?)null;
        var flagsWritten = false;
        var offset = start;
        while (true)
        {
            int opcode = stored[offset];
            var length = TokenLength(opcode);
            if (opcode <= ExplicitHeight)
            {
                WriteFloors(output, change);
                if (!flagsWritten) WriteFlags(output, flags);
                if (change.Height is { } height) output.Write([ExplicitHeight, (byte)height]);
                else output.Write(stored, offset, end - offset);
                return;
            }
            var isFlags = opcode is > LastOverlayOpcode and <= LastFlagsOpcode;
            var replaced = opcode <= LastOverlayOpcode ? change.Overlay is not null : isFlags ? flags is not null : change.Underlay is not null;
            if (!replaced) output.Write(stored, offset, length);
            else if (isFlags && !flagsWritten)
            {
                // The new value takes the first stored token's place; any later flag token is dropped.
                WriteFlags(output, flags);
                flagsWritten = true;
            }
            offset += length;
        }
    }

    private static void WriteFlags(MemoryStream output, int? flags)
    {
        if (flags is > 0) output.WriteByte((byte)(flags.Value + LastOverlayOpcode));
    }

    /// <summary>The value of the tile's last flag token, which is the one the client keeps.</summary>
    private static int StoredFlags(byte[] stored, int offset)
    {
        var flags = 0;
        while (true)
        {
            int opcode = stored[offset];
            if (opcode <= ExplicitHeight) return flags;
            if (opcode is > LastOverlayOpcode and <= LastFlagsOpcode) flags = opcode - LastOverlayOpcode;
            offset += TokenLength(opcode);
        }
    }

    private static void WriteFloors(MemoryStream output, MapTerrainTileChange change)
    {
        if (change.Underlay is > 0 and var underlay) output.WriteByte((byte)(underlay + LastFlagsOpcode));
        if (change.Overlay is > 0 and var overlay)
            output.Write([(byte)(2 + (change.OverlayShape ?? 0) * 4 + (change.OverlayRotation ?? 0)), (byte)overlay]);
    }
}
