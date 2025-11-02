using System;
using System.IO;

namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IArchive : IDisposable
    {
        /// <summary>
        /// Gets a specific member file entry by its identifier.
        /// </summary>
        /// <param name="subFileId">The identifier of the member file to retrieve.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the data of the requested member file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the archive has not yet been decoded.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="subFileId"/> is out of the valid range for the archive's entries.</exception>
        MemoryStream GetEntry(int subFileId);

        /// <summary>
        /// Gets the array of member file entries in this archive, represented as in-memory streams.
        /// This property is populated after the archive is decoded.
        /// </summary>
        MemoryStream[]? Entries { get; }
    }
}