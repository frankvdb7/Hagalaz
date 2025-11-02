using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Models
{
    /// <summary>
    /// An <see cref="Archive"/> is a file within the cache that can have multiple member
    /// files inside it.
    /// </summary>
    public class Archive : IArchive
    {
        /// <summary>
        /// Gets the array of member file entries in this archive, represented as in-memory streams.
        /// This property is populated after the archive is decoded.
        /// </summary>
        public MemoryStream[]? Entries { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Archive" /> class.
        /// </summary>
        /// <param name="size">The number of member file entries this archive will hold.</param>
        public Archive(int size) => Entries = new MemoryStream[size];


        /// <summary>
        /// Gets a specific member file entry by its identifier.
        /// </summary>
        /// <param name="subFileId">The identifier of the member file to retrieve.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the data of the requested member file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the archive has not yet been decoded.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="subFileId"/> is out of the valid range for the archive's entries.</exception>
        public MemoryStream GetEntry(int subFileId)
        {
            if (Entries == null)
            {
                throw new InvalidOperationException($"{nameof(Archive)} is not decoded");
            }
            if (subFileId < 0 || subFileId >= Entries.Length)
                throw new ArgumentOutOfRangeException(nameof(subFileId));
            return Entries[subFileId];
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing || Entries == null)
            {
                return;
            }
            
            foreach (var t in Entries)
            {
                t.Dispose();
            }

            Entries = null;
        }
    }
}
