namespace Hagalaz.Data.Entities
{
    public partial class CharactersMusicPlaylist
    {
        public uint MasterId { get; set; }
        public byte PlaylistToggled { get; set; }
        public byte ShuffleToggled { get; set; }
        public string Playlist { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}
