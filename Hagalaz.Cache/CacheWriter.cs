using Hagalaz.Cache.Utilities;
using Hagalaz.Security;
using System.IO;

namespace Hagalaz.Cache
{
    public class CacheWriter : ICacheWriter
    {
        private readonly IFileStore _store;
        private readonly IReferenceTableProvider _referenceTableProvider;
        private readonly IContainerDecoder _containerFactory;
        private readonly IReferenceTableCodec _referenceTableCodec;

        public CacheWriter(IFileStore store, IReferenceTableProvider referenceTableProvider, IContainerDecoder containerFactory, IReferenceTableCodec referenceTableCodec)
        {
            _store = store;
            _referenceTableProvider = referenceTableProvider;
            _containerFactory = containerFactory;
            _referenceTableCodec = referenceTableCodec;
        }

        public void Write(int indexId, int fileId, IContainer container)
        {
            /* we don't want people reading/manipulating these manually */
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");

            /* increment the container's version */
            container.Version++;

            /* decode the reference table for this index */
            var table = _referenceTableProvider.ReadReferenceTable(indexId);

            /* update the version and checksum for this file */
            var entry = table.GetEntry(fileId);
            if (entry == null)
            {
                /* create a new entry for the file */
                entry = new ReferenceTableEntry(fileId);
                table.AddEntry(fileId, entry);
            }
            entry.Version = container.Version;

            /* grab the bytes we need for the checksum */
            var bytes = container.Encode(false);

            /* calculate the new CRC checksum */
            entry.Crc32 = BufferUtilities.GetCrcChecksum(bytes, 0, bytes.Length);

            /* calculate and update the whirlpool digest if we need to */
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
            {
                entry.WhirlpoolDigest = Whirlpool.GenerateDigest(bytes, 0, bytes.Length);
            }

            /* save the file itself */
            using (var fileStream = new MemoryStream(bytes))
            {
                _store.Write(indexId, fileId, fileStream);
            }

            /* update the reference table version */
            table.Version++;

            /* save the reference table */
            using var oldReferenceTableContainerStream = _store.Read(255, indexId);
            using var oldReferenceTableContainer = _containerFactory.Decode(oldReferenceTableContainerStream);

            using (var referenceTableContainer = new Container(oldReferenceTableContainer.CompressionType, _referenceTableCodec.Encode(table)))
            {
                using (MemoryStream refStream = new MemoryStream(referenceTableContainer.Encode()))
                    _store.Write(255, indexId, refStream);
            }
        }
    }
}
