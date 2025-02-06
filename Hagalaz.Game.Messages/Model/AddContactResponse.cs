namespace Hagalaz.Game.Messages.Model
{
    public class AddContactResponse
    {
        public const byte Opcode = 21;

        public enum ResponseCode : byte
        {
            Success = 0,
            NotFound = 1,
            Fail = 2
        }

        public record SettingsDto
        {
            public ContactAvailability? Availability { get; init; }
        }

        public record ContactDto
        {
            public uint Id { get; init; }
            public string DisplayName { get; init; } = default!;
            public string? PreviousDisplayName { get; init; }
            public object? Rank { get; init; }
            public int? WorldId { get; init; }
            public ContactType ContactType { get; init; }
            public bool? IsFriend { get; init; }
            public SettingsDto? Settings { get; init; }
        }
        
        public uint CharacterId { get; init; }
        public ContactDto Contact { get; init; } = default!;
        public ResponseCode Response { get; init; }
    }
}