namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITypeEncoder<in T> where T : IType
    {
        #region Methods
        /// <summary>
        /// Encodes the specified type identifier.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <param name="type">The type.</param>
        void Encode(int typeId, T type);
        #endregion Methods
    }
}
