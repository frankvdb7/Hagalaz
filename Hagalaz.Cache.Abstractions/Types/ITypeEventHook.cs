namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines a contract for an event hook that allows for post-processing actions
    /// after a collection of cacheable types has been decoded.
    /// </summary>
    /// <typeparam name="T">The type of the objects that were decoded.</typeparam>
    /// <remarks>
    /// This interface is useful for scenarios where additional logic needs to be executed
    /// after all instances of a particular type are loaded, such as resolving dependencies
    /// between types or performing validation.
    /// </remarks>
    public interface ITypeEventHook<in T>
    {
        /// <summary>
        /// A callback method that is invoked after all instances of type <typeparamref name="T"/> have been decoded.
        /// </summary>
        /// <param name="provider">The type provider that was used to decode the types.</param>
        /// <param name="types">An array containing all the decoded instances of type <typeparamref name="T"/>.</param>
        void AfterDecode(ITypeProvider<T> provider, T[] types);
    }
}
