using System;
using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="ChecksumTable"/> stores checksums and versions of
    /// <see cref="ReferenceTable"/>. When encoded in a <see cref="Container"/> and prepended
    /// with the file type and id it is more commonly known as the client's
    /// "update keys".
    /// </summary>
    public class ChecksumTable : IDisposable, IEnumerable<ChecksumTableEntry>
    {
        /// <summary>
        /// The files.
        /// </summary>
        private readonly ChecksumTableEntry[] _entries;

        /// <summary>
        /// Contains count of entries.
        /// </summary>
        /// <value>The entry count.</value>
        public int Count => _entries.Length;

        public ChecksumTableEntry this[int index] => _entries[index];

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumTable"/> class.
        /// </summary>
        /// <param name="entryCount">The entry count.</param>
        public ChecksumTable(int entryCount) => _entries = new ChecksumTableEntry[entryCount];


        /// <summary>
        /// Sets the file.
        /// </summary>
        /// <param name="entryID">The file identifier.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="System.IndexOutOfRangeException"></exception>
        public void SetEntry(int entryID, ChecksumTableEntry file)
        {
            if (entryID < 0 || entryID >= Count)
                throw new IndexOutOfRangeException();
            _entries[entryID] = file;
        }

        public IEnumerator<ChecksumTableEntry> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Attempts to dispose the table.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed code.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // no-op
            }
        }
    }
}