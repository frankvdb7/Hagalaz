using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;

namespace Hagalaz.Game.Messages.Protocol
{
    public class SetWorldInfoMessage : RaidoMessage
    {
        public record WorldLocationInfoDto
        {
            public required string Name { get; init; }
            public required int Flag { get; init; }
        }

        public record WorldSettingsInfoDto
        {
            public required bool IsMembersOnly { get; init; }
            public required bool IsQuickChatEnabled { get; init; }
            public required bool IsPvP { get; init; }
            public required bool IsLootShareEnabled { get; init; }
            public required bool IsHighlighted { get; init; }
        }

        public record WorldInfoDto
        {
            public required int Id { get; init; }
            public required string Name { get; init; }
            public required string IpAddress { get; init; }
            public required WorldLocationInfoDto Location { get; init; }
            public required WorldSettingsInfoDto Settings { get; init; }
        }

        public record WorldCharacterInfoDto
        {
            public required int Id { get; init; }
            public required bool Online { get; init; }
            public required int CharacterCount { get; init; }
        }

        [MemberNotNullWhen(true, nameof(WorldInfos), nameof(LocationInfos))]
        public bool FullUpdate { get; init; }
        public required IReadOnlyList<WorldInfoDto> WorldInfos { get; init; }
        public required IReadOnlyList<WorldLocationInfoDto> LocationInfos { get; init; }
        public required IReadOnlyList<WorldCharacterInfoDto> CharacterInfos { get; init; }
        public required int Checksum { get; init; }
    }
}
