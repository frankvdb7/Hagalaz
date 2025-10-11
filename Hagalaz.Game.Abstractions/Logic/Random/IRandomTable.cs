using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// Defines a contract for a table of random objects, from which one or more entries can be selected based on their probabilities.
    /// </summary>
    /// <typeparam name="TEntry">The type of the entries in the table, which must implement <see cref="IRandomObject"/>.</typeparam>
    public interface IRandomTable<out TEntry> : IRandomObject
        where TEntry : IRandomObject
    {
        /// <summary>
        /// Gets the maximum number of entries that can be selected from this table in a single operation.
        /// </summary>
        int MaxResultCount { get; }

        /// <summary>
        /// Gets a value indicating whether the number of selected entries should be randomized, up to <see cref="MaxResultCount"/>.
        /// If false, the generator will attempt to select exactly <see cref="MaxResultCount"/> entries.
        /// </summary>
        bool RandomizeResultCount { get; }

        /// <summary>
        /// Gets a read-only list of all possible entries in this table.
        /// </summary>
        IReadOnlyList<TEntry> Entries { get; }

        /// <summary>
        /// Gets a read-only list of modifiers that can alter the properties of the selected entries.
        /// </summary>
        IReadOnlyList<IRandomObjectModifier> Modifiers { get; }
    }
}
