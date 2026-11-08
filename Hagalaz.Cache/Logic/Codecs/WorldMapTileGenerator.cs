using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Derives a region's world map tiles from its stored terrain and objects.</summary>
public static class WorldMapTileGenerator
{
    private const int Tiles = 4096;
    private const int FlagBridge = 2;
    private const int FlagUpperVisible = 8;

    public static WorldMapTile[] Generate(TerrainRenderPlane[] terrain, IEnumerable<MapRenderPlacement> placements, Func<int, ObjectType?> definition)
    {
        var objects = placements.Where(placement => Includes(placement, definition(placement.Id)))
            .ToLookup(placement => (placement.Plane, placement.X * 64 + placement.Y));
        var tiles = new WorldMapTile[Tiles];
        for (var tile = 0; tile < Tiles; tile++)
        {
            var layers = new List<WorldMapLayer> { Layer(terrain[0], tile, objects[(0, tile)]) };
            for (var plane = 1; plane < terrain.Length; plane++)
                if (HasUpperLayer(terrain[plane], tile, objects[(plane, tile)].Any())) layers.Add(Layer(terrain[plane], tile, objects[(plane, tile)]));
            tiles[tile] = new WorldMapTile(layers);
        }
        return tiles;
    }

    /// <summary>Walls draw as lines; anything else appears only when it has a map icon or map sprite.</summary>
    public static bool Includes(MapRenderPlacement placement, ObjectType? definition)
    {
        if (definition is null) return false;
        return placement.Shape switch
        {
            0 or 2 or 3 or 9 => true,
            1 or 10 or 11 or 22 => definition.MapIcon != -1 || definition.MapSpriteType != -1,
            _ => false,
        };
    }

    private static bool HasUpperLayer(TerrainRenderPlane plane, int tile, bool hasObjects) =>
        (plane.Flags[tile] & (FlagUpperVisible | FlagBridge)) != 0 && (hasObjects || plane.Underlays[tile] != 0 || plane.Overlays[tile] != 0);

    private static WorldMapLayer Layer(TerrainRenderPlane plane, int tile, IEnumerable<MapRenderPlacement> objects)
    {
        var overlay = plane.Overlays[tile];
        return new WorldMapLayer(plane.Underlays[tile], overlay, overlay == 0 ? 0 : Pack(plane.OverlayShapes[tile], plane.OverlayRotations[tile]),
            objects.Select(placement => new WorldMapObject(placement.Id, Pack(placement.Shape, placement.Rotation))).ToArray());
    }

    private static int Pack(int shape, int rotation) => rotation << 6 | shape;
}
