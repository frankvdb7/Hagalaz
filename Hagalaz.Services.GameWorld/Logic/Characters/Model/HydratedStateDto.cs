using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedStateDto
    {
        public record HydratedStateExDto
        {
            public required string Id { get; init; }
            public required int TicksLeft { get; init; }
        }

        public required IReadOnlyList<HydratedStateExDto> StatesEx { get; init; }
    }
}
