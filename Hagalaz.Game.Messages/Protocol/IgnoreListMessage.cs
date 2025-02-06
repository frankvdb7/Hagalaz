using System.Collections.Generic;
using Hagalaz.Game.Messages.Protocol.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class IgnoreListMessage : RaidoMessage
    {
        public IReadOnlyList<ContactDto> Ignores { get; init; } = default!;
    }
}
