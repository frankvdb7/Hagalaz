using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines a contract for a collection of creatures, providing methods for adding, removing, and accessing them by index.
    /// </summary>
    /// <typeparam name="TCreature">The type of creature stored in the collection, which must implement <see cref="ICreature"/>.</typeparam>
    public interface ICreatureCollection<in TCreature> : IEnumerable, IEnumerable<TCreature> where TCreature : ICreature
    {
        /// <summary>
        /// Gets the creature at the specified index.
        /// </summary>
        /// <param name="index">The index of the creature to retrieve.</param>
        /// <returns>The <see cref="ICreature"/> at the specified index, or <c>null</c> if the index is out of range or empty.</returns>
        TCreature? this[int index] { get; }

        /// <summary>
        /// Gets the maximum number of creatures that can be stored in the collection.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the current number of creatures in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds a creature to the collection.
        /// </summary>
        /// <param name="creature">The creature to add.</param>
        /// <returns><c>true</c> if the creature was added successfully; otherwise, <c>false</c>.</returns>
        bool Add(TCreature creature);

        /// <summary>
        /// Removes a creature from the collection.
        /// </summary>
        /// <param name="creature">The creature to remove.</param>
        /// <returns><c>true</c> if the creature was removed successfully; otherwise, <c>false</c>.</returns>
        bool Remove(TCreature creature);
    }
}
