using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawStandardMapMessage : DrawMapMessage
    {
        public required IReadOnlyList<int[]> VisibleRegionXteaKeys { get; init; } = default!;
    }
}
