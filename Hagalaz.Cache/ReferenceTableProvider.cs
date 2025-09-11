using System.IO;

namespace Hagalaz.Cache
{
    public class ReferenceTableProvider : IReferenceTableProvider
    {
        private readonly IFileStore _store;
        private readonly IContainerDecoder _containerFactory;
        private readonly IReferenceTableDecoder _referenceTableFactory;
        private readonly IReferenceTable[] _referenceTables;

        public ReferenceTableProvider(IFileStore store, IContainerDecoder containerFactory, IReferenceTableDecoder referenceTableFactory)
        {
            _store = store;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
            _referenceTables = new IReferenceTable[store.IndexFileCount];
        }

        public IReferenceTable ReadReferenceTable(int indexId)
        {
            if (indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            if (indexId == 255)
                throw new IOException("Checksum tables can not be read by this method!");

            return _referenceTables[indexId] ??= DecodeReferenceTable(indexId);
        }

        private IReferenceTable DecodeReferenceTable(int indexId)
        {
            using var fileBuffer = _store.Read(255, indexId);
            using var container = _containerFactory.Decode(fileBuffer);
            return _referenceTableFactory.Decode(container.Data, true);
        }
    }
}
