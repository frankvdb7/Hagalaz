using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IChecksumTable : IEnumerable<IChecksumTableEntry>
    {
        /// <summary>
        /// Gets the number of entries in the checksum table.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets the <see cref="IChecksumTableEntry"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the entry to get.</param>
        /// <returns>The entry at the specified index.</returns>
        IChecksumTableEntry this[int index] { get; }
    }
}