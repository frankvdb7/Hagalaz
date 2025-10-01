namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITypeEventHook<in T>
    {
        /// <summary>
        /// Occurs after all the types are decoded
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="types">The types.</param>
        void AfterDecode(ITypeProvider<T> provider, T[] types);
    }
}
