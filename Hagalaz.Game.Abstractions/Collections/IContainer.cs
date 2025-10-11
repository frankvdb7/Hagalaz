using System;
using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines a generic contract for a read-only container that holds a collection of objects.
    /// It provides indexed access and a fixed capacity.
    /// </summary>
    /// <typeparam name="T">The type of elements in the container.</typeparam>
    public interface IContainer<out T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Gets the element at the specified index in the container.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of the valid range for the container.</exception>
        T this[int index] { get; }

        /// <summary>
        /// Gets the total number of elements the container can hold.
        /// </summary>
        int Capacity { get; }
    }
}