using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
{
    /// <summary>
    /// A provider for decoding item type definitions from the cache.
    /// </summary>
    public class ItemTypeProvider : ITypeProvider<IItemType>
    {
        private readonly ItemTypeData _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly IItemTypeCodec _codec;
        private readonly ITypeFactory<IItemType> _typeFactory;
        private readonly ITypeEventHook<IItemType>? _typeEventHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemTypeProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache API.</param>
        /// <param name="codec">The item type codec.</param>
        /// <param name="typeFactory">The item type factory.</param>
        /// <param name="typeEventHook">The item type event hook.</param>
        public ItemTypeProvider(ICacheAPI cache, IItemTypeCodec codec, ITypeFactory<IItemType> typeFactory, ITypeEventHook<IItemType>? typeEventHook = null)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _typeEventHook = typeEventHook;
        }

        /// <inheritdoc />
        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        /// <inheritdoc />
        public IItemType Get(int typeId)
        {
            return TypeProvider<IItemType, ItemTypeData>.DecodeFromCache(this, _cache, _typeData, _typeFactory, _codec, _typeEventHook, typeId);
        }

        /// <inheritdoc />
        public IItemType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new IItemType[endTypeId - startTypeId];
            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                types[typeId - startTypeId] = Get(typeId);
            }
            return types;
        }

        /// <inheritdoc />
        public IItemType[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}
