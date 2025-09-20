using System;
using System.Collections.Generic;

namespace Hagalaz.Cache
{
    /// <summary>
    /// Represents a child entry within an <see cref="ReferenceTableEntry"/> in the
    /// <see cref="ReferenceTable"/>.
    /// </summary>
    public class ReferenceTableChildEntry
    {
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        public int Id { get; set; } = -1;

        /// <summary>
        /// Contains the index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTableChildEntry"/> class.
        /// </summary>
        public ReferenceTableChildEntry(int index) => Index = index;
    }

    /// <summary>
    /// Represents a single entry within a <see cref="ReferenceTable"/>.
    /// </summary>
    public class ReferenceTableEntry
    {
        /// <summary>
        /// The entries.
        /// </summary>
        private SortedList<int, ReferenceTableChildEntry>? _entries;

        /// <summary>
        /// Contains file crc32.
        /// </summary>
        public int Crc32 { get; set; }

        /// <summary>
        /// Contains the version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Contains file id.
        /// </summary>
        public int Id { get; set; } = -1;

        /// <summary>
        /// Contains whirlpool digest of this file.
        /// </summary>
        public byte[] WhirlpoolDigest { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Contains the maximum capacity for child entries.
        /// </summary>
        public int Capacity
        {
            get
            {
                if (_entries == null)
                {
                    throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
                }

                return _entries.Capacity;
            }
        }

        /// <summary>
        /// Contains the number of child entries.
        /// </summary>
        public int Count
        {
            get
            {
                if (_entries == null)
                {
                    throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
                }

                return _entries.Count;
            }
        }

        /// <summary>
        /// Contains the entries.
        /// </summary>
        public IEnumerable<KeyValuePair<int, ReferenceTableChildEntry>> Entries
        {
            get
            {
                if (_entries == null)
                {
                    throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
                }

                return _entries;
            }
        }

        /// <summary>
        /// Contains the index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceTableEntry" /> class.
        /// </summary>
        /// <param name="index">The index.</param>
        public ReferenceTableEntry(int index) => Index = index;

        /// <summary>
        /// Gets the child entry with the specified id.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <returns>The entry, or null if it does not exist.</returns>
        public ReferenceTableChildEntry? GetEntry(int childId)
        {
            if (_entries == null)
            {
                throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
            }

            _entries.TryGetValue(childId, out var entry);
            return entry;
        }

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <param name="entry">The entry.</param>
        public void AddEntry(int childId, ReferenceTableChildEntry entry)
        {
            if (_entries == null)
            {
                throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
            }

            _entries.Add(childId, entry);
        }

        /// <summary>
        /// Determines whether the specified child identifier contains entry.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <returns>
        ///   <c>true</c> if the specified child identifier contains entry; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsEntry(int childId)
        {
            if (_entries == null)
            {
                throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");
            }

            return _entries.ContainsKey(childId);
        }

        /// <summary>
        /// Initializes the entries.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public void InitializeEntries(int capacity) => _entries = new SortedList<int, ReferenceTableChildEntry>(capacity);
    }
}