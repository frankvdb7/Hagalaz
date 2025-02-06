namespace Hagalaz.Characters.Messages.Model
{
    public record MusicDto(int[] UnlockedMusicIds, int[] PlaylistMusicIds, bool IsPlaylistToggled, bool IsShuffleToggled);
}
