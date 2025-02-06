using System.Collections.Generic;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record Notes
    {
        public record Note
        {
            public int Id { get; init; }
            public int Color { get; init; }
            public string Text { get; init; } = string.Empty;
        }

        public IReadOnlyList<Note> AllNotes { get; init; } = new List<Note>();
    }
}
