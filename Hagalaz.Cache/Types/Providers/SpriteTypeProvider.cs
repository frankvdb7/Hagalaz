using System.Collections.Generic;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
{
    public class SpriteTypeProvider : ITypeProvider<ISpriteType>
    {
        private readonly SpriteTypeData _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly ISpriteTypeCodec _codec;
        private readonly ITypeFactory<ISpriteType> _typeFactory;
        private readonly ITypeEventHook<ISpriteType>? _typeEventHook;

        public SpriteTypeProvider(ICacheAPI cache, ISpriteTypeCodec codec, ITypeFactory<ISpriteType> typeFactory, ITypeEventHook<ISpriteType>? typeEventHook = null)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _typeEventHook = typeEventHook;
        }

        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        public ISpriteType Get(int typeId)
        {
            return TypeProvider<ISpriteType, SpriteTypeData>.DecodeFromCache(this, _cache, _typeData, _typeFactory, _codec, _typeEventHook, typeId);
        }

        public ISpriteType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new List<ISpriteType>();
            for (var id = startTypeId; id < endTypeId; id++)
                types.Add(Get(id));

            return types.ToArray();
        }

        public ISpriteType[] GetAll() => GetRange(0, ArchiveSize);
    }
}
