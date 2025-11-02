namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IIndex
    {
        /// <summary>
        /// Contains the size of the file in bytes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Contains the number of the first sector that contains the file.
        /// </summary>
        int SectorID { get; }
    }
}