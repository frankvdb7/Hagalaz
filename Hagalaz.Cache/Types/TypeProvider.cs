using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
{
    public class TypeProvider<T, TD> : ITypeProvider<T> where T : IType where TD : ITypeData, new()
    {
        private readonly TD _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly ITypeFactory<T> _typeFactory;
        private readonly ITypeEventHook<T>? _typeEventHook;
        private readonly ITypeProvider<T>? _customDecoder;

        public int ArchiveSize => _customDecoder?.ArchiveSize ?? _typeData.GetArchiveSize(_cache);

        public TypeProvider(ICacheAPI cache, ITypeFactory<T> typeFactory, ITypeEventHook<T>? typeEventHook = null, ITypeProvider<T>? customDecoder = null)
        {
            _cache = cache;
            _typeFactory = typeFactory;
            _typeEventHook = typeEventHook;
            _customDecoder = customDecoder;
        }

        public T Get(int typeId)
        {
            if (_customDecoder != null)
            {
                return _customDecoder.Get(typeId);
            }

            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            var type = _typeFactory.CreateType(typeId);
            var archiveId = _typeData.GetArchiveId(typeId);
            var archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
            var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
            if (entry == null)
            {
                return type;
            }

            type.Decode(archive.GetEntry(entry.Index));
            _typeEventHook?.AfterDecode(this, new[] { type });
            return type;
        }

        public T[] GetRange(int startTypeId, int endTypeId)
        {
            if (_customDecoder != null)
            {
                return _customDecoder.GetRange(startTypeId, endTypeId);
            }

            var types = new T[endTypeId - startTypeId];
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            Archive? archive = null;
            var currentArchiveId = -1;
            for (var typeId = startTypeId; typeId < endTypeId; typeId++)
            {
                var type = types[typeId] = _typeFactory.CreateType(typeId);
                var archiveId = _typeData.GetArchiveId(typeId);
                if (archiveId != currentArchiveId)
                {
                    archive = _cache.ReadArchive(_typeData.IndexId, archiveId);
                    currentArchiveId = archiveId;
                }
                var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
                if (entry == null || archive == null)
                {
                    continue;
                }

                types[typeId].Decode(archive.GetEntry(entry.Index));
            }
            _typeEventHook?.AfterDecode(this, types);

            return types;
        }

        public T[] GetAll()
        {
            if (_customDecoder != null)
            {
                return _customDecoder.GetAll();
            }

            var count = _typeData.GetArchiveSize(_cache);
            return GetRange(0, count);
        }
    }
}