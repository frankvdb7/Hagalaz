using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Types.Providers
{
    public class ReferenceTableProvider : IReferenceTableProvider
    {
        private readonly IFileStore _store;
        private readonly IContainerDecoder _containerFactory;
        private readonly IReferenceTableCodec _referenceTableFactory;
        private readonly IReferenceTable?[] _referenceTables;
        private readonly object[] _referenceTableLocks;

        public ReferenceTableProvider(IFileStore store, IContainerDecoder containerFactory, IReferenceTableCodec referenceTableFactory)
        {
            _store = store;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
            var indexFileCount = store.IndexFileCount;
            _referenceTables = new IReferenceTable?[indexFileCount];
            _referenceTableLocks = new object[indexFileCount];
            for (var indexId = 0; indexId < indexFileCount; indexId++)
            {
                _referenceTableLocks[indexId] = new object();
            }
        }

        public IReferenceTable ReadReferenceTable(int indexId)
        {
            if (indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            if (indexId == 255)
                throw new IOException("Checksum tables can not be read by this method!");

            var cachedTable = Volatile.Read(ref _referenceTables[indexId]);
            if (cachedTable != null)
            {
                return cachedTable;
            }

            lock (_referenceTableLocks[indexId])
            {
                cachedTable = _referenceTables[indexId];
                if (cachedTable == null)
                {
                    cachedTable = DecodeReferenceTable(indexId);
                    Volatile.Write(ref _referenceTables[indexId], cachedTable);
                }

                return cachedTable;
            }
        }

        public void Invalidate(int indexId)
        {
            if (indexId >= 0 && indexId < _referenceTables.Length && indexId != 255)
            {
                lock (_referenceTableLocks[indexId])
                {
                    Volatile.Write(ref _referenceTables[indexId], null);
                }
            }
        }

        private IReferenceTable DecodeReferenceTable(int indexId)
        {
            using var fileBuffer = _store.Read(255, indexId);
            using var container = _containerFactory.Decode(fileBuffer);
            return _referenceTableFactory.Decode(container.Data, true);
        }
    }
}
