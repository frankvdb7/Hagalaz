using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TD">The type of the d.</typeparam>
    public class TypeEncoder<T, TD> : ITypeEncoder<T> where T : IType where TD : ITypeData, new()
    {
        /// <summary>
        /// The type data
        /// </summary>
        private TD _typeData = new TD();
        /// <summary>
        /// The cache
        /// </summary>
        private readonly ICacheAPI _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDecoder{T, TD}" /> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public TypeEncoder(ICacheAPI cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Encodes the specified encode.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <param name="type">The type.</param>
        public void Encode(int typeId, T type)
        {
            var fileId = _typeData.GetArchiveId(typeId);
            var oldContainer = _cache.ReadContainer(_typeData.IndexId, fileId);
            using (var newContainer = new Container(oldContainer.CompressionType, type.Encode()))
            {
                _cache.Write(_typeData.IndexId, fileId, newContainer);
            }
        }
    }
}
