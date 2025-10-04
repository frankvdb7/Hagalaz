using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types
{
    public class TypeEncoder<T, TD> : ITypeEncoder<T> where T : IType where TD : ITypeData, new()
    {
        private readonly TD _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly ITypeCodec<T> _codec;

        public TypeEncoder(ICacheAPI cache, ITypeCodec<T> codec)
        {
            _cache = cache;
            _codec = codec;
        }

        public void Encode(int typeId, T type)
        {
            var fileId = _typeData.GetArchiveId(typeId);
            var oldContainer = _cache.ReadContainer(_typeData.IndexId, fileId);
            using (var newContainer = new Container(oldContainer.CompressionType, _codec.Encode(type)))
            {
                _cache.Write(_typeData.IndexId, fileId, newContainer);
            }
        }
    }
}
