using System;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Collections.Extensions
{
    /// <summary>
    /// Provides a set of static extension methods for various collection types,
    /// enhancing their functionality with common operations.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and
        /// returns the zero-based index of the first occurrence within the entire <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to search.</param>
        /// <param name="predicate">The <see cref="Func{T, TResult}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by
        /// <paramref name="predicate"/>, if found; otherwise, –1.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            var tSources = source as TSource[] ?? source.ToArray();
            for (var i = 0; i < tSources.Length; i++)
                if (predicate(tSources.ElementAt(i)))
                    return i;
            return -1;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the <see cref="HashSet{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the hash set.</typeparam>
        /// <param name="this">The <see cref="HashSet{T}"/> to which the items will be added.</param>
        /// <param name="items">The collection whose elements should be added to the hash set.</param>
        /// <returns>
        /// <c>true</c> if all items from the collection were successfully added;
        /// <c>false</c> if at least one item was already present in the hash set.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        public static bool AddRange<T>(this HashSet<T> @this, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            return items.Aggregate(true, (current, item) => current & @this.Add(item));
        }

        /// <summary>
        /// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to iterate over.</param>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the sequence.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="action"/> is null.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(action);

            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}