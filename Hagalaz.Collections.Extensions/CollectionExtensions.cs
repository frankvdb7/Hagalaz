using System;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Collections.Extensions
{
    /// <summary>
    /// This static class contains extension methods for collections.
    /// We do not use a namespace so that classes won't need to import this extension class.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Gets the index of a certain element.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The first occurring index of the found predicate.</returns>
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
        /// Adds the range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this">The this.</param>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        public static bool AddRange<T>(this HashSet<T> @this, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            return items.Aggregate(true, (current, item) => current & @this.Add(item));
        }

        /// <summary>
        /// Fors the each.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        /// <exception cref="System.ArgumentNullException">
        /// source
        /// or
        /// action
        /// </exception>
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