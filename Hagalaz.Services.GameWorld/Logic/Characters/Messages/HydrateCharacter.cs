using System;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Messages
{
    public record HydrateCharacter(uint MasterId)
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }
}
