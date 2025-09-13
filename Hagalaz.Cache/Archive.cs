using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    /// <summary>
    /// An <see cref="Archive"/> is a file within the cache that can have multiple member
    /// files inside it.
    /// </summary>
    public class Archive : IDisposable
    {
        /// <summary>
        /// The array of entries in this archive.
        /// </summary>
        public MemoryStream[]? Entries { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Archive" /> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public Archive(int size) => Entries = new MemoryStream[size];


        /// <summary>
        /// Gets the entry.
        /// </summary>
        /// <param name="subFileId">The sub file identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public MemoryStream GetEntry(int subFileId)
        {
            if (Entries == null)
            {
                throw new InvalidOperationException($"{nameof(Archive)} is not decoded");
            }
            if (subFileId < 0 || subFileId >= Entries.Length)
                throw new ArgumentOutOfRangeException();
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
