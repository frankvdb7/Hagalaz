using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Data;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Providers
{
    public class TypeProvider<T, TD> : ITypeProvider<T> where T : IType where TD : ITypeData, new()
    {
        private readonly TD _typeData = new();
        private readonly ICacheAPI _cache;
        private readonly ITypeFactory<T> _typeFactory;
        private readonly ITypeEventHook<T>? _typeEventHook;
        private readonly ITypeProvider<T>? _customDecoder;

        public int ArchiveSize => _customDecoder?.ArchiveSize ?? _typeData.GetArchiveSize(_cache);

        private readonly ITypeCodec<T> _codec;

        public TypeProvider(ICacheAPI cache, ITypeFactory<T> typeFactory, ITypeCodec<T> codec, ITypeEventHook<T>? typeEventHook = null, ITypeProvider<T>? customDecoder = null)
        {
            _cache = cache;
            _typeFactory = typeFactory;
            _codec = codec;
            _typeEventHook = typeEventHook;
            _customDecoder = customDecoder;
        }

        public T Get(int typeId)
        {
            if (_customDecoder != null)
            {
                return _customDecoder.Get(typeId);
            }

            return DecodeFromCache(this, _cache, _typeData, _typeFactory, _codec, _typeEventHook, typeId);
        }

        internal static T DecodeFromCache(
            ITypeProvider<T> provider,
            ICacheAPI cache,
            ITypeData typeData,
            ITypeFactory<T> typeFactory,
            ITypeCodec<T> codec,
            ITypeEventHook<T>? typeEventHook,
            int typeId)
        {
            var archiveId = typeData.GetArchiveId(typeId);
            var entry = cache.ReadReferenceTable(typeData.IndexId).GetEntry(archiveId, typeData.GetArchiveEntryId(typeId));
            if (entry == null)
            {
                return typeFactory.CreateType(typeId);
            }

            using var stream = cache.Read(typeData.IndexId, archiveId, entry.Index);
            var type = codec.Decode(typeId, stream);
            typeEventHook?.AfterDecode(provider, [type]);
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
            IArchive? archive = null;
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

                types[typeId - startTypeId] = _codec.Decode(typeId, archive.GetEntry(entry.Index));
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
