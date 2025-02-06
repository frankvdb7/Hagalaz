using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections
{
    public interface IContainer<out T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Gets the object by the specified array index.
        /// </summary>
        /// <value>
        /// The <see cref="Item"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>
        /// Returns the object.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        T this[int index] { get; }
        /// <summary>
        /// The capacity for the amount of objects the container can hold.
        /// </summary>
        /// <value>The capacity.</value>
        int Capacity { get; }
    }
}