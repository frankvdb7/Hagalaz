using System.Collections.Generic;

namespace Hagalaz.Game.Messages.Model
{
    public class GetContactsResponse
    {
        public const byte Opcode = 6;

        public record ContactSettings
        {
            public ContactAvailability Availability { get; init; }
        }
        
        public record Contact
        {
            public uint MasterId { get; init; }
            public string DisplayName { get; init;  } = default!;
            public string? PreviousDisplayName { get; init;  }
            public object? Rank { get; init;  }
            public int? WorldId { get; init;  }
            public bool? IsFriend { get; init; }
            public ContactSettings? Settings { get; init; }
        }
        
        public uint CharacterId { get; init;  }
        public IReadOnlyList<Contact> Friends { get; init;  } = default!;
        public IReadOnlyList<Contact> Ignores { get; init;  } = default!;
    }
}