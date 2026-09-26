using System.Collections.Generic;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record State
    {
        public record StateEx
        {
            public required string Id { get; init; }
            public required int TicksLeft { get; init; }
        }

        public required IReadOnlyList<StateEx> StatesEx { get; init; }
    }
}
