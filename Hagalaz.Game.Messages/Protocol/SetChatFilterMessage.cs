using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Features.Chat;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetChatFilterMessage : RaidoMessage
    {
        public Availability? PublicFilter { get; init; }
        public Availability? TradeFilter { get; init; }
        public Availability? PrivateFilter { get; init; }

        [MemberNotNullWhen(true, nameof(PrivateFilter))]
        [MemberNotNullWhen(false, nameof(PublicFilter))]
        [MemberNotNullWhen(false, nameof(TradeFilter))]
        public bool HasPrivateFilter => PrivateFilter != null;
    }
}
