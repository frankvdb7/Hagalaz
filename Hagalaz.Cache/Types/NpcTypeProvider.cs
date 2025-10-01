using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// A provider for decoding NPC type definitions from the cache.
    /// </summary>
    public class NpcTypeProvider : ITypeProvider<INpcType>
    {
        private readonly NpcTypeData _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly INpcTypeCodec _codec;
        private readonly ITypeFactory<INpcType> _typeFactory;
        private readonly ITypeEventHook<INpcType>? _typeEventHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="NpcTypeProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache API.</param>
        /// <param name="codec">The NPC type codec.</param>
        /// <param name="typeFactory">The NPC type factory.</param>
        /// <param name="typeEventHook">The NPC type event hook.</param>
        public NpcTypeProvider(ICacheAPI cache, INpcTypeCodec codec, ITypeFactory<INpcType> typeFactory, ITypeEventHook<INpcType>? typeEventHook = null)
        {
            _cache = cache;
            _codec = codec;
            _typeFactory = typeFactory;
            _typeEventHook = typeEventHook;
        }

        /// <inheritdoc />
        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        /// <inheritdoc />
        public INpcType Get(int typeId)
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
        public INpcType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new INpcType[endTypeId - startTypeId];
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            Archive? archive = null;
            var currentArchiveId = -1;

            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                var archiveId = _typeData.GetArchiveId(typeId);
                if (archiveId != currentArchiveId)
                {
                    archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
                    currentArchiveId = archiveId;
                }

                var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
                if (entry == null || archive == null)
                {
                    types[typeId - startTypeId] = _typeFactory.CreateType(typeId);
                    continue;
                }

                var stream = archive.GetEntry(entry.Index);
                types[typeId - startTypeId] = _codec.Decode(typeId, stream);
            }

            _typeEventHook?.AfterDecode(this, types);
            return types;
        }

        /// <inheritdoc />
        public INpcType[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}