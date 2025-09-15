using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Hagalaz.Cache.Extensions;
using Hagalaz.Security;

namespace Hagalaz.Cache
{
    /// <summary>
    /// A <see cref="ChecksumTable"/> stores checksums and versions of
    /// <see cref="ReferenceTable"/>. When encoded in a <see cref="Container"/> and prepended
    /// with the file type and id it is more commonly known as the client's
    /// "update keys".
    /// </summary>
    public class ChecksumTable : IDisposable
    {
        /// <summary>
        /// The files.
        /// </summary>
        internal ChecksumTableEntry[] _entries;

        public ChecksumTableEntry[] Entries => _entries;

        /// <summary>
        /// Contains count of entries.
        /// </summary>
        /// <value>The entry count.</value>
        public int Count
        {
            get
            {
                if (_entries == null)
                {
                    throw new InvalidOperationException($"{nameof(ChecksumTable)} is not decoded");
                }

                return _entries.Length;
            }
        }

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
                _entries = null!;
            }
        }
    }
}