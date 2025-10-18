namespace Hagalaz.Cache
{
    /// <summary>
    /// Defines the contract for an archive decoder, which extracts member files from a raw data container.
    /// </summary>
    public interface IArchiveDecoder
    {
        /// <summary>
        /// Decodes the archive data from the provided container.
        /// </summary>
        /// <param name="container">The container holding the raw, compressed archive data.</param>
        /// <param name="size">The number of member file entries expected in the archive.</param>
        /// <returns>A decoded <see cref="Archive"/> with its member file entries populated.</returns>
        Archive Decode(IContainer container, int size);
    }
}
