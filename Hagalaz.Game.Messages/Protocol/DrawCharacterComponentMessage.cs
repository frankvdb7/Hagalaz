using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class DrawCharacterComponentMessage : RaidoMessage
    {
        public required int ComponentId { get; init; }
        public required int ChildId { get; init; }
        public int? CharacterName { get; init; }
        public int? CharacterIndex { get; init; }
        [MemberNotNullWhen(false, nameof(CharacterIndex), nameof(CharacterName))]
        public required bool DrawSelf { get; init; }
    }
}
