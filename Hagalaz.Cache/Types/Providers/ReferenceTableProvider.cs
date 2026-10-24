using System;
using System.IO;
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

        public ReferenceTableProvider(IFileStore store, IContainerDecoder containerFactory, IReferenceTableCodec referenceTableFactory)
        {
            _store = store;
            _containerFactory = containerFactory;
            _referenceTableFactory = referenceTableFactory;
        }

        public IReferenceTable ReadReferenceTable(int indexId)
        {
            if (indexId >= _store.IndexFileCount)
                throw new FileNotFoundException();
            if (indexId == 255)
                throw new IOException("Checksum tables can not be read by this method!");
            if (indexId < 0)
                throw new IndexOutOfRangeException();

            return DecodeReferenceTable(indexId);
        }

        private IReferenceTable DecodeReferenceTable(int indexId)
        {
            using var fileBuffer = _store.Read(255, indexId);
            using var container = _containerFactory.Decode(fileBuffer);
            return _referenceTableFactory.Decode(container.Data, true);
        }
    }
}
