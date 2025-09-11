using Hagalaz.Cache.Utilities;
using System.IO;

namespace Hagalaz.Cache
{
    public class CacheWriter : ICacheWriter
    {
        private readonly IFileStore _store;
        private readonly IReferenceTableProvider _referenceTableProvider;

        public CacheWriter(IFileStore store, IReferenceTableProvider referenceTableProvider)
        {
            _store = store;
            _referenceTableProvider = referenceTableProvider;
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
            var whirlpool = new byte[64];
            if (table.Flags.HasFlag(ReferenceTableFlags.Digests))
                whirlpool = Whirlpool.GenerateDigest(bytes, 0, bytes.Length);
            entry.WhirlpoolDigest = whirlpool;

            /* update the reference table version */
            table.Version++;

            /* save the reference table */
            using (var referenceTableContainer = new Container(CompressionType.None, table.Encode()))
            {
                using (MemoryStream refStream = new MemoryStream(referenceTableContainer.Encode()))
                    _store.Write(255, indexId, refStream);
            }

            /* save the file itself */
            using (MemoryStream fileStream = new MemoryStream(bytes))
                _store.Write(indexId, fileId, fileStream);
        }
    }
}
