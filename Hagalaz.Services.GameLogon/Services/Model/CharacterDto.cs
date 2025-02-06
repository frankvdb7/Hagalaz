using System;
using System.Collections.Generic;

namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record CharacterDto
    {
        public record Claim
        {
            public string Name { get; init; } = default!;
        }
        public uint Id { get; init; }
        public string DisplayName { get; init; } = default!;
        public string? PreviousDisplayName { get; init; }
        public int? WorldId { get; init; }
        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
    }
}