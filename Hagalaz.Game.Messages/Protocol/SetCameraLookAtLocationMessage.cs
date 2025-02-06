using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetCameraLookAtLocationMessage : RaidoMessage
    {
        public int? LocalX { get; init; }
        public int? LocalY { get; init; }
        public int? Z { get; init; }
        public int? SpeedX { get; init; }
        public int? SpeedY { get; init; }
        [MemberNotNullWhen(false, nameof(LocalX), nameof(LocalY), nameof(Z), nameof(SpeedX), nameof(SpeedY))]
        public bool Reset { get; init; }
    }
}
