using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;
using System.Collections.Generic;

namespace Hagalaz.Cache.Types
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
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            var archiveId = _typeData.GetArchiveId(typeId);
            var archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
            var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
            if (entry == null)
            {
                return _typeFactory.CreateType(typeId);
            }
            var type = _codec.Decode(archive.GetEntry(entry.Index));
            _typeEventHook?.AfterDecode(this, new[] { type });

            return type;
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