namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITypeDecoder<out T> where T : IType
    {
        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <value>
        /// The size of the archive.
        /// </value>
        int ArchiveSize { get; }

        /// <summary>
        /// Decodes the specified type factory.
        /// </summary>
        /// <returns></returns>
        T[] DecodeAll();
        /// <summary>
        /// Decodes the types.
        /// </summary>
        /// <param name="startTypeId">The start type identifier.</param>
        /// <param name="endTypeId">The end type identifier.</param>
        /// <returns></returns>
        T[] DecodeRange(int startTypeId, int endTypeId);
        /// <summary>
        /// Decodes this instance.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        T Decode(int typeId);
    }
}
