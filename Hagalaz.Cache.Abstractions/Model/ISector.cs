namespace Hagalaz.Cache.Abstractions.Model
{
    public interface ISector
    {
        int IndexID { get; }
        int FileID { get; }
        int ChunkID { get; }
        int NextSectorID { get; }
    }
}