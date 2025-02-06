using System;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record Music
    {
        public int[] UnlockedMusicIds { get; init; } = Array.Empty<int>();
        public int[] PlaylistMusicIds { get; init; } = Array.Empty<int>();
        public bool IsPlaylistToggled { get; init; }
        public bool IsShuffleToggled { get; init; }
    }
}
