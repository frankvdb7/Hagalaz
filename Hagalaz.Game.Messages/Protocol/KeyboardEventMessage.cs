using System.Collections.Generic;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class KeyboardEventMessage : RaidoMessage
    {
        public class KeyInfo
        {
            public required int EventCode { get; init; }
            public required int DeltaTime { get; init; }
        }

        public required IReadOnlyList<KeyInfo> Keys { get; init; } = default!;
    }
}
