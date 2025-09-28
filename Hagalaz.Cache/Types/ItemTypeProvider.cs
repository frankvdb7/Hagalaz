using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
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
        public IItemType Decode(int typeId)
        {
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            var archiveId = _typeData.GetArchiveId(typeId);
            var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
            if (entry == null)
            {
                return _typeFactory.CreateType(typeId);
            }

            var archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
            var stream = archive.GetEntry(entry.Index);
            var type = _codec.Decode(typeId, stream);

            _typeEventHook?.AfterDecode(this, new[] { type });
            return type;
        }

        /// <inheritdoc />
        public IItemType[] DecodeRange(int startTypeId, int endTypeId)
        {
            var types = new IItemType[endTypeId - startTypeId];
            for (var i = 0; i < types.Length; i++)
            {
                var typeId = startTypeId + i;
                types[i] = Decode(typeId);
            }
            return types;
        }

        /// <inheritdoc />
        public IItemType[] DecodeAll()
        {
            var count = ArchiveSize;
            return DecodeRange(0, count);
        }
    }
}