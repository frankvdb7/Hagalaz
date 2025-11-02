using System;
using System.Collections;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Models
{
    /// <summary>
    /// A <see cref="ChecksumTable"/> stores checksums and versions of
    /// <see cref="ReferenceTable"/>. When encoded in a <see cref="Container"/> and prepended
    /// with the file type and id it is more commonly known as the client's
    /// "update keys".
    /// </summary>
    public class ChecksumTable : IChecksumTable
    {
        /// <summary>
        /// The array of entries in this checksum table.
        /// </summary>
        private readonly IChecksumTableEntry[] _entries;

        /// <summary>
        /// Gets the number of entries in the checksum table.
        /// </summary>
        public int Count => _entries.Length;

        /// <summary>
        /// Gets the <see cref="ChecksumTableEntry"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the entry to get.</param>
        /// <returns>The entry at the specified index.</returns>
        public IChecksumTableEntry this[int index] => _entries[index];

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumTable"/> class.
        /// </summary>
        /// <param name="entryCount">The number of entries the checksum table will have.</param>
        public ChecksumTable(int entryCount) => _entries = new IChecksumTableEntry[entryCount];


        /// <summary>
        /// Sets the checksum entry for a specific index.
        /// </summary>
        /// <param name="entryID">The identifier of the entry (typically the cache index ID).</param>
        /// <param name="file">The checksum entry data.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the <paramref name="entryID"/> is out of the valid range.</exception>
        public void SetEntry(int entryID, ChecksumTableEntry file)
        {
            if (entryID < 0 || entryID >= Count)
                throw new IndexOutOfRangeException();
            _entries[entryID] = file;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of checksum entries.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<IChecksumTableEntry> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}