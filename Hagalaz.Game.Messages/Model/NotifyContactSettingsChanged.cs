namespace Hagalaz.Game.Messages.Model
{
    public class NotifyContactSettingsChanged
    {
        public const byte Opcode = 25;

        public record SettingsDto(ContactAvailability Availability)
        {
        }

        public record ContactDto(uint MasterId, string DisplayName, int WorldId, SettingsDto Settings)
        {
            public string? PreviousDisplayName { get; init; }
        }

        public ContactDto Contact { get; init; } = default!;
    }
}