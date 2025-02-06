using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TCreature">The type of the creature.</typeparam>
    /// <seealso cref="System.Collections.Generic.IEnumerable{TCreature}" />
    public interface ICreatureCollection<TCreature> : IEnumerable, IEnumerable<TCreature> where TCreature : ICreature
    {
        /// <summary>
        /// Gets the <see cref="TCreature"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="TCreature"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        TCreature? this[int index] { get; }
        /// <summary>
        /// Gets the capacity of the collection.
        /// </summary>
        int Capacity { get; }
        /// <summary>
        /// Gets the creature count of the collection.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Adds the specified creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns></returns>
        bool Add(TCreature creature);
        /// <summary>
        /// Removes the specified creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns></returns>
        bool Remove(TCreature creature);
    }
}
