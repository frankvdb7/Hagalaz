using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Reads the client's world map `area` member and rewrites individual region blocks of it.</summary>
public static class WorldMapAreaCodec
{
    private const int RegionBlockKind = 0;
    private const int ChunkBlockTiles = 64;
    private const int RegionBlockTiles = 4096;
    private const int EmptyEntry = 62;
    private const int LiteralEntry = 63;
    private const int ComplexTile = 1;
    private const int OverlayTile = 2;
    private const int HasOverlays = 8;
    private const int HasObjects = 16;

    public static WorldMapAreaFile Read(byte[] data)
    {
        var offset = 0;
        var underlays = ReadPalette(data, ref offset);
        var overlays = ReadPalette(data, ref offset);
        var paletteEnd = offset;
        var palettes = new WorldMapAreaFile(underlays, overlays, paletteEnd, []);
        var blocks = new List<WorldMapBlock>();
        while (offset < data.Length)
        {
            var start = offset;
            var kind = ReadByte(data, ref offset);
            var regionX = ReadByte(data, ref offset);
            var regionY = ReadByte(data, ref offset);
            var tiles = RegionBlockTiles;
            if (kind != RegionBlockKind)
            {
                offset += 2;
                tiles = ChunkBlockTiles;
            }
            var contentStart = offset;
            for (var tile = 0; tile < tiles; tile++) ReadTile(data, ref offset, palettes);
            blocks.Add(new WorldMapBlock(kind, regionX, regionY, start, contentStart, offset));
        }
        return new WorldMapAreaFile(underlays, overlays, paletteEnd, blocks);
    }

    public static WorldMapTile[] DecodeRegion(byte[] data, WorldMapAreaFile area, WorldMapBlock block)
    {
        if (block.Kind != RegionBlockKind) throw new InvalidDataException("Only whole-region world map blocks can be decoded.");
        var offset = block.ContentStart;
        var tiles = new WorldMapTile[RegionBlockTiles];
        for (var tile = 0; tile < tiles.Length; tile++) tiles[tile] = ReadTile(data, ref offset, area);
        return tiles;
    }

    /// <summary>Replaces whole-region blocks and copies everything else exactly as stored.</summary>
    public static byte[] ReplaceRegions(byte[] data, WorldMapAreaFile area, IReadOnlyDictionary<WorldMapBlock, WorldMapTile[]> replacements)
    {
        using var output = new MemoryStream(data.Length);
        var offset = 0;
        foreach (var block in area.Blocks)
        {
            if (!replacements.TryGetValue(block, out var tiles)) continue;
            output.Write(data, offset, block.ContentStart - offset);
            foreach (var tile in tiles) WriteTile(output, tile, area);
            offset = block.End;
        }
        output.Write(data, offset, data.Length - offset);
        return output.ToArray();
    }

    /// <summary>Reads a `details` entry's map name and its parts.</summary>
    public static (string Map, IReadOnlyList<WorldMapAreaPart> Parts) ReadDetails(byte[] data)
    {
        var offset = 0;
        var map = ReadString(data, ref offset);
        ReadString(data, ref offset);
        offset += 11;
        var parts = new WorldMapAreaPart[ReadByte(data, ref offset)];
        for (var index = 0; index < parts.Length; index++)
        {
            var plane = ReadByte(data, ref offset);
            var values = new int[8];
            for (var value = 0; value < values.Length; value++) values[value] = ReadByte(data, ref offset) << 8 | ReadByte(data, ref offset);
            parts[index] = new WorldMapAreaPart(plane, values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
        }
        return (map, parts);
    }

    private static string ReadString(byte[] data, ref int offset)
    {
        var start = offset;
        while (ReadByte(data, ref offset) != 0) { }
        return System.Text.Encoding.Latin1.GetString(data, start, offset - start - 1);
    }

    private static int[] ReadPalette(byte[] data, ref int offset)
    {
        var palette = new int[ReadByte(data, ref offset)];
        for (var entry = 0; entry < palette.Length; entry++) palette[entry] = ReadByte(data, ref offset);
        return palette;
    }

    private static int ReadByte(byte[] data, ref int offset) =>
        offset < data.Length ? data[offset++] : throw new EndOfStreamException("World map area ends inside a block.");

    private static WorldMapTile ReadTile(byte[] data, ref int offset, WorldMapAreaFile area)
    {
        var flags = ReadByte(data, ref offset);
        return (flags & ComplexTile) == 0 ? ReadSimpleTile(data, ref offset, flags, area) : ReadComplexTile(data, ref offset, flags);
    }

    private static WorldMapTile ReadSimpleTile(byte[] data, ref int offset, int flags, WorldMapAreaFile area)
    {
        var entry = flags >> 2 & 0x3f;
        if (entry == EmptyEntry) return WorldMapTile.Empty;
        var overlay = (flags & OverlayTile) != 0;
        var literal = entry == LiteralEntry ? ReadByte(data, ref offset) : 0;
        var underlay = overlay ? ReadByte(data, ref offset) : 0;
        var value = entry == LiteralEntry ? literal : (overlay ? area.Overlays : area.Underlays)[entry];
        return new WorldMapTile([overlay ? new WorldMapLayer(underlay, value, 0, []) : new WorldMapLayer(value, 0, 0, [])]);
    }

    private static WorldMapTile ReadComplexTile(byte[] data, ref int offset, int flags)
    {
        var layers = new WorldMapLayer[(flags >> 1 & 3) + 1];
        for (var index = 0; index < layers.Length; index++)
        {
            var underlay = ReadByte(data, ref offset);
            var (overlay, info) = (flags & HasOverlays) != 0 ? (ReadByte(data, ref offset), ReadByte(data, ref offset)) : (0, 0);
            var objects = new WorldMapObject[(flags & HasObjects) != 0 ? ReadByte(data, ref offset) : 0];
            for (var entry = 0; entry < objects.Length; entry++) objects[entry] = new WorldMapObject(ReadBigSmart(data, ref offset), ReadByte(data, ref offset));
            layers[index] = new WorldMapLayer(underlay, overlay, info, objects);
        }
        return new WorldMapTile(layers);
    }

    private static int ReadBigSmart(byte[] data, ref int offset)
    {
        if (offset >= data.Length) throw new EndOfStreamException("World map area ends inside an object.");
        if ((data[offset] & 0x80) != 0)
            return (ReadByte(data, ref offset) & 0x7f) << 24 | ReadByte(data, ref offset) << 16 | ReadByte(data, ref offset) << 8 | ReadByte(data, ref offset);
        var value = ReadByte(data, ref offset) << 8 | ReadByte(data, ref offset);
        return value == 32767 ? -1 : value;
    }

    private static void WriteTile(MemoryStream output, WorldMapTile tile, WorldMapAreaFile area)
    {
        if (tile.Layers.Count == 0) output.WriteByte(EmptyEntry << 2);
        else if (tile.Layers.Count == 1 && tile.Layers[0] is { Objects.Count: 0, Info: 0 } layer) WriteSimpleTile(output, layer, area);
        else WriteComplexTile(output, tile);
    }

    private static void WriteSimpleTile(MemoryStream output, WorldMapLayer layer, WorldMapAreaFile area)
    {
        var overlay = layer.Overlay != 0;
        var value = overlay ? layer.Overlay : layer.Underlay;
        var entry = Array.IndexOf(overlay ? area.Overlays : area.Underlays, value);
        // Entries 62 and 63 are reserved, whatever a palette holds at those positions.
        var literal = entry is < 0 or >= EmptyEntry;
        output.WriteByte((byte)((literal ? LiteralEntry : entry) << 2 | (overlay ? OverlayTile : 0)));
        if (literal) output.WriteByte((byte)value);
        if (overlay) output.WriteByte((byte)layer.Underlay);
    }

    private static void WriteComplexTile(MemoryStream output, WorldMapTile tile)
    {
        var overlays = tile.Layers.Any(layer => layer.Overlay != 0 || layer.Info != 0);
        var objects = tile.Layers.Any(layer => layer.Objects.Count > 0);
        output.WriteByte((byte)(ComplexTile | (tile.Layers.Count - 1) << 1 | (overlays ? HasOverlays : 0) | (objects ? HasObjects : 0)));
        foreach (var layer in tile.Layers)
        {
            output.WriteByte((byte)layer.Underlay);
            if (overlays)
            {
                output.WriteByte((byte)layer.Overlay);
                output.WriteByte((byte)layer.Info);
            }
            if (!objects) continue;
            output.WriteByte((byte)layer.Objects.Count);
            foreach (var entry in layer.Objects)
            {
                output.WriteBigSmart(entry.Id);
                output.WriteByte((byte)entry.Data);
            }
        }
    }

}
