using System.Collections.Generic;

namespace Hagalaz.Cache.Types;

// Arrays use x * dimension + y. Heights remain in downward-positive legacy units.
public sealed record TerrainRenderPlane(int[] Heights, int[] Flags, int[] Underlays, int[] Overlays, int[] OverlayShapes, int[] OverlayRotations);
public sealed record MapRenderPlane(int?[] CornerHeights, int[] Flags, int[] Underlays, int[] Overlays, int[] OverlayShapes, int[] OverlayRotations);
public sealed record MapRenderPlacement(int Id, int Shape, int Rotation, int X, int Y, int Plane);
public sealed record FloorRenderDefinition(int Id, bool Overlay, int? Rgb, int TextureId, int? SecondaryRgb, int TextureScale);
public sealed record MapAtmosphere(int LightRgb, float Intensity, float PositiveResponse, float NegativeResponse, int DirectionX, int DirectionY, int DirectionZ,
    int SkyRgb, int FogValue);
public sealed record MapRenderRegion(int Id, int RegionX, int RegionY, string TerrainStatus, string LandscapeStatus,
    IReadOnlyList<MapRenderPlane> Planes, IReadOnlyList<MapRenderPlacement> Objects, IReadOnlyList<FloorRenderDefinition> Floors, IReadOnlyList<string> Warnings,
    MapAtmosphere? Atmosphere = null);
