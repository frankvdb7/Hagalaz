namespace Hagalaz.Game.Messages.Model
{
    public class NotifyContactAdded
    {
        public const byte Opcode = 27;

        public record ContactDto(uint Id, string DisplayName, string? PreviousDisplayName);

        public ContactDto Master { get; init; } = default!;
        public ContactDto Contact { get; init; } = default!;
    }
}