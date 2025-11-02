using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Models
{
    /// <summary>
    /// Represents a child entry within an <see cref="ReferenceTableEntry"/> in the
    /// <see cref="ReferenceTable"/>.
    /// </summary>
    public class ReferenceTableChildEntry : IReferenceTableChildEntry
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
    public class ReferenceTableEntry : IReferenceTableEntry
    {
        /// <summary>
        /// The entries.
        /// </summary>
        private SortedList<int, IReferenceTableChildEntry>? _entries;

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
        public byte[] WhirlpoolDigest { get; set; } = [];

        /// <summary>
        /// Contains the maximum capacity for child entries.
        /// </summary>
        public int Capacity => _entries?.Capacity ?? throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");

        /// <summary>
        /// Contains the number of child entries.
        /// </summary>
        public int Count => _entries?.Count ?? throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");

        /// <summary>
        /// Contains the entries.
        /// </summary>
        public IEnumerable<KeyValuePair<int, IReferenceTableChildEntry>> Entries =>
            _entries ?? throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");

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
        public IReferenceTableChildEntry? GetEntry(int childId)
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
        public void AddEntry(int childId, IReferenceTableChildEntry entry)
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
        public bool ContainsEntry(int childId) =>
            _entries?.ContainsKey(childId) ?? throw new InvalidOperationException($"{nameof(ReferenceTableEntry)} is not initialized");

        /// <summary>
        /// Initializes the entries.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public void InitializeEntries(int capacity) => _entries = new SortedList<int, IReferenceTableChildEntry>(capacity);
    }
}