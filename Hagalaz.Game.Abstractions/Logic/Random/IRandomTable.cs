using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// This interface contains the properties an object must have to be a valid table.
    /// </summary>
    /// <typeparam name="TEntry">The type of the input.</typeparam>
    /// <seealso cref="IRandomObject" />
    public interface IRandomTable<out TEntry> : IRandomObject
        where TEntry : IRandomObject
    {
        /// <summary>
        /// The maximum number of entries expected in the Results. The final count of items in the result may be lower
        /// if some of the entries may return a null result (no drop)
        /// </summary>
        int MaxResultCount { get; }
        /// <summary>
        /// Gets a value indicating whether [randomize real drop count].
        /// The loot count is calculated with:
        /// (MaxCount) minus (Always count of all content (also iterating of possible IRandomTables))
        /// </summary>
        /// <value>
        /// <c>true</c> if [randomize real drop count]; otherwise, <c>false</c>.
        /// </value>
        bool RandomizeResultCount { get; }
        /// <summary>
        /// Contains the entries of this table.
        /// </summary>
        IReadOnlyList<TEntry> Entries { get; }
        /// <summary>
        /// Contains the modifiers of this table.
        /// </summary>
        IReadOnlyList<IRandomObjectModifier> Modifiers { get; }
    }
}
