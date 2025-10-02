using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// A provider for decoding object type definitions from the cache.
    /// </summary>
    public class ObjectTypeProvider : ITypeProvider<IObjectType>
    {
        private readonly ObjectTypeData _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly IObjectTypeCodec _codec;
        private readonly ITypeFactory<IObjectType> _typeFactory;
        private readonly ITypeEventHook<IObjectType>? _typeEventHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTypeProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache API.</param>
        /// <param name="codec">The object type codec.</param>
        /// <param name="typeFactory">The object type factory.</param>
        /// <param name="typeEventHook">The object type event hook.</param>
        public ObjectTypeProvider(ICacheAPI cache, IObjectTypeCodec codec, ITypeFactory<IObjectType> typeFactory, ITypeEventHook<IObjectType>? typeEventHook = null)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _typeEventHook = typeEventHook;
        }

        /// <inheritdoc />
        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        /// <inheritdoc />
        public IObjectType Get(int typeId)
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
        public IObjectType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new IObjectType[endTypeId - startTypeId];
            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                types[typeId - startTypeId] = Get(typeId);
            }
            return types;
        }

        /// <inheritdoc />
        public IObjectType[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}