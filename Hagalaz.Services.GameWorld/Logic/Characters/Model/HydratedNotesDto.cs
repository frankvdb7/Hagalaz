using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedNotesDto
    {
        public record NoteDto
        {
            public int Id { get; init; }
            public int Color { get; init; }
            public string Text { get; init; } = string.Empty;
        }

        public IReadOnlyList<NoteDto> Notes { get; init; } = new List<NoteDto>();
    }
}
