namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedMusicDto(int[] UnlockedMusicIds, int[] PlaylistMusicIds, bool IsPlaylistToggled, bool IsShuffleToggled);
}
