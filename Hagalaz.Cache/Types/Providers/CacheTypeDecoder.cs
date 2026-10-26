using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Data;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Providers
{
    internal sealed class CacheTypeDecoder<T>
        where T : IType
    {
        private readonly ICacheAPI _cache;
        private readonly ITypeData _typeData;
        private readonly ITypeFactory<T> _typeFactory;
        private readonly ITypeCodec<T> _codec;
        private readonly ITypeEventHook<T>? _typeEventHook;

        public CacheTypeDecoder(
            ICacheAPI cache,
            ITypeData typeData,
            ITypeFactory<T> typeFactory,
            ITypeCodec<T> codec,
            ITypeEventHook<T>? typeEventHook)
        {
            _cache = cache;
            _typeData = typeData;
            _typeFactory = typeFactory;
            _codec = codec;
            _typeEventHook = typeEventHook;
        }

        public T Decode(ITypeProvider<T> owner, int typeId)
        {
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            var archiveId = _typeData.GetArchiveId(typeId);

            return DecodeMapped(owner, table, archiveId, typeId);
        }

        public T DecodeWithRequiredArchive(ITypeProvider<T> owner, int typeId)
        {
            var table = _cache.ReadReferenceTable(_typeData.IndexId);
            var archiveId = _typeData.GetArchiveId(typeId);

            if (table.GetEntry(archiveId) is null)
            {
                throw new FileNotFoundException();
            }

            return DecodeMapped(owner, table, archiveId, typeId);
        }

        private T DecodeMapped(ITypeProvider<T> owner, IReferenceTable table, int archiveId, int typeId)
        {
            var entry = table.GetEntry(archiveId, _typeData.GetArchiveEntryId(typeId));
            if (entry is null)
            {
                return _typeFactory.CreateType(typeId);
            }

            using var stream = _cache.Read(_typeData.IndexId, archiveId, entry.Index);
            var type = _codec.Decode(typeId, stream);
            _typeEventHook?.AfterDecode(owner, [type]);

            return type;
        }
    }
}
