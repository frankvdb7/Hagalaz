using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TD">The type of the d.</typeparam>
    /// <seealso cref="ITypeDecoder{T}" />
    public class TypeDecoder<T, TD>  : ITypeDecoder<T> where T : IType where TD : ITypeData, new()
    {
        /// <summary>
        /// The type data
        /// </summary>
        private readonly TD _typeData = new();
        /// <summary>
        /// The cache
        /// </summary>
        private readonly ICacheAPI _cache;
        /// <summary>
        /// The type factory
        /// </summary>
        private readonly ITypeFactory<T> _typeFactory;
        /// <summary>
        /// The type initializer
        /// </summary>
        private readonly ITypeEventHook<T>? _typeEventHook;

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <value>
        /// The size of the archive.
        /// </value>
        public int ArchiveSize => _typeData.GetArchiveSize(_cache);

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDecoder{T, TD}"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="typeFactory">The type factory.</param>
        public TypeDecoder(ICacheAPI cache, ITypeFactory<T> typeFactory)
        {
            _cache = cache;
            _typeFactory = typeFactory;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDecoder{T, Y}" /> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="typeFactory">The type factory.</param>
        /// <param name="typeEventHook">The type initializer.</param>
        public TypeDecoder(ICacheAPI cache, ITypeFactory<T> typeFactory, ITypeEventHook<T> typeEventHook)
            : this(cache, typeFactory)
        {
            _typeEventHook = typeEventHook;
        }

        /// <summary>
        /// Decodes this instance.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public T Decode(int typeId)
        {
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
            _typeEventHook?.AfterDecode(this, [type]);
            return type;
        }

        /// <summary>
        /// Decodes the types.
        /// </summary>
        /// <param name="startTypeId">The start type identifier.</param>
        /// <param name="endTypeId">The end type identifier.</param>
        /// <returns></returns>
        public T[] DecodeRange(int startTypeId, int endTypeId)
        {
            var count = _typeData.GetArchiveSize(_cache);
            var types = new T[count];
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
        /// <summary>
        /// Decodes types.
        /// </summary>
        /// <returns></returns>
        public T[] DecodeAll()
        {
            var count = _typeData.GetArchiveSize(_cache);
            return DecodeRange(0, count);
        }
    }
}
