using System;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Messages
{
    public record CharacterDehydrated
    {
        public required uint MasterId { get; init; }
        public required Guid CorrelationId { get; init; }
    }
}
