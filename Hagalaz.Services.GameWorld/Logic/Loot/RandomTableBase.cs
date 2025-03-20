using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TEntryType">The type of the content type.</typeparam>
    /// <seealso cref="IRandomTable{TEntryType}" />
    public abstract class RandomTableBase<TEntryType> : IRandomTable<TEntryType>
        where TEntryType : IRandomObject
    {
        private readonly List<TEntryType> _entries = [];
        private readonly List<IRandomObjectModifier> _modifiers = [];

        /// <summary>
        /// Contains Id of the drop table.
        /// </summary>
        public required int Id { get; init; }

        /// <summary>
        /// Contains name of the drop table.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Contains the entries
        /// </summary>
        public required IReadOnlyList<TEntryType> Entries
        {
            get => _entries;
            init => _entries.AddRange(value);
        }

        /// <summary>
        /// Contains the modifiers
        /// </summary>
        public IReadOnlyList<IRandomObjectModifier> Modifiers
        {
            get => _modifiers;
            init => _modifiers.AddRange(value);
        }

        /// <summary>
        /// Gets or sets the probability for this object to be (part of) the result
        /// </summary>
        public double Probability =>
            Entries
                .Where(e => e.Enabled)
                .Select(e => e.Probability)
                .DefaultIfEmpty()
                .Average();

        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        public bool Always => Entries.Any(e => e.Always && e.Enabled);

        /// <summary>
        /// Gets or sets a value indicating whether this IRandomObject is enabled.
        /// Only enabled entries can be part of the result of a ILootTable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// The maximum number of entries expected in the Results. The final count of items in the result may be lower
        /// if some of the entries may return a null result (no drop)
        /// </summary>
        public int MaxResultCount { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether [randomize real drop count].
        /// The loot count is calculated with:
        /// (MaxCount) minus (Always count of all content (also iterating of possible IRandomTables))
        /// </summary>
        /// <value>
        /// <c>true</c> if [randomize real drop count]; otherwise, <c>false</c>.
        /// </value>
        public bool RandomizeResultCount { get; init; }

        public void AddEntry(TEntryType entry)
        {
            IRandomObject obj = entry;
            if (obj != this || !_entries.Contains(entry))
            {
                _entries.Add(entry);
            }
        }

        public void AddModifier(IRandomObjectModifier modifier)
        {
            if (!_modifiers.Contains(modifier))
            {
                _modifiers.Add(modifier);
            }
        }
    }
}