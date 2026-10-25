using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Types.Providers
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
        private readonly CacheTypeDecoder<INpcType> _decoder;

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
            _decoder = new CacheTypeDecoder<INpcType>(_cache, _typeData, _typeFactory, _codec, _typeEventHook);
        }

        /// <inheritdoc />
        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        /// <inheritdoc />
        public INpcType Get(int typeId)
        {
            return _decoder.Decode(this, typeId);
        }

        /// <inheritdoc />
        public INpcType[] GetRange(int startTypeId, int endTypeId)
        {
            var types = new INpcType[endTypeId - startTypeId];
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            IArchive? archive = null;
            var currentArchiveId = -1;
            var typeId = startTypeId;

            try
            {
                while (typeId < endTypeId)
                {
                    var archiveId = _typeData.GetArchiveId(typeId);
                    if (archiveId != currentArchiveId)
                    {
                        var previousArchive = archive;
                        archive = null;
                        previousArchive?.Dispose();
                        archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
                        currentArchiveId = archiveId;
                    }

                    var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
                    if (entry is not null && archive is not null)
                    {
                        var stream = archive.GetEntry(entry.Index);
                        types[typeId - startTypeId] = _codec.Decode(typeId, stream);
                    }
                    else
                    {
                        types[typeId - startTypeId] = _typeFactory.CreateType(typeId);
                    }

                    typeId++;
                }

                _typeEventHook?.AfterDecode(this, types);
                return types;
            }
            finally
            {
                archive?.Dispose();
            }
        }

        /// <inheritdoc />
        public INpcType[] GetAll()
        {
            var count = ArchiveSize;
            return GetRange(0, count);
        }
    }
}
