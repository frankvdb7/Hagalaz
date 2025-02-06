using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetCameraShakeMessage : RaidoMessage
    {
        public int? Index { get; init; }
        public int? UpDelta { get; init; }
        public int? DownDelta { get; init; }
        public int? LeftDelta { get; init; }
        public int? RightDelta { get; init; }
        [MemberNotNullWhen(false, nameof(Index), nameof(UpDelta), nameof(DownDelta), nameof(LeftDelta), nameof(RightDelta))]
        public bool Reset { get; init; }
    }
}
