using System.Collections.Generic;

namespace Hagalaz.Cache.Types;

/// <summary>An object on a world map tile; <paramref name="Data"/> is `rotation &lt;&lt; 6 | shape`.</summary>
public sealed record WorldMapObject(int Id, int Data);

/// <summary>One floor of a world map tile; <paramref name="Info"/> packs the overlay as `rotation &lt;&lt; 6 | shape`.</summary>
public sealed record WorldMapLayer(int Underlay, int Overlay, int Info, IReadOnlyList<WorldMapObject> Objects);

public sealed record WorldMapTile(IReadOnlyList<WorldMapLayer> Layers)
{
    public static readonly WorldMapTile Empty = new([]);
}

/// <summary>Where one block sits in an `area` member.</summary>
public sealed record WorldMapBlock(int Kind, int RegionX, int RegionY, int Start, int ContentStart, int End);

/// <summary>One rectangle of an area: where its tiles really are, and where the world map shows them.</summary>
public sealed record WorldMapAreaPart(int Plane, int MinX, int MinY, int MaxX, int MaxY, int DisplayMinX, int DisplayMinY, int DisplayMaxX, int DisplayMaxY)
{
    public bool ContainsReal(int x, int y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
}

public sealed record WorldMapAreaFile(int[] Underlays, int[] Overlays, int PaletteEnd, IReadOnlyList<WorldMapBlock> Blocks);
