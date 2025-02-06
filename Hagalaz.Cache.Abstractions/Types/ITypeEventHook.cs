namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITypeEventHook<in T> where T : IType
    {
        /// <summary>
        /// Occurs after all the types are decoded
        /// </summary>
        /// <param name="decoder">The decoder.</param>
        /// <param name="types">The types.</param>
        void AfterDecode(ITypeDecoder<T> decoder, T[] types);
    }
}
