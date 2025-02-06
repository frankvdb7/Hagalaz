namespace Hagalaz.Game.Messages.Model
{
    public class NotifyContactRemoved
    {
        public const byte Opcode = 28;
        
        public record ContactDto(uint MasterId, string DisplayName, string? PreviousDisplayName);

        public ContactDto Master { get; init; } = default!;
        public ContactDto Contact { get; init; } = default!;
    }
}