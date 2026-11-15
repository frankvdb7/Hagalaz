using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Writes the landscape format read by <see cref="MapRenderDecoder.DecodeLandscape"/>, using raw source planes.</summary>
public static class MapLandscapeEncoder
{
    /// <summary>The order the format stores placements in; placements sharing a location keep their relative order.</summary>
    public static MapRenderPlacement[] Canonicalize(IEnumerable<MapRenderPlacement> placements) =>
        placements.OrderBy(placement => placement.Id).ThenBy(Location).ToArray();

    public static MemoryStream Encode(IEnumerable<MapRenderPlacement> placements)
    {
        var stream = new MemoryStream();
        var previousId = -1;
        foreach (var group in Canonicalize(placements).GroupBy(placement => placement.Id))
        {
            stream.WriteHugeSmart(group.Key - previousId);
            previousId = group.Key;
            var previousLocation = 0;
            foreach (var placement in group)
            {
                var location = Location(placement);
                stream.WriteSmart(location - previousLocation + 1);
                previousLocation = location;
                stream.WriteByte((byte)((placement.Shape << 2) | placement.Rotation));
            }
            stream.WriteSmart(0);
        }
        stream.WriteHugeSmart(0);
        stream.Position = 0;
        return stream;
    }

    private static int Location(MapRenderPlacement placement) => (placement.Plane << 12) | (placement.X << 6) | placement.Y;
}
