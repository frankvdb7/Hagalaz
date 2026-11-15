using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;
using Hagalaz.Security;

namespace Hagalaz.Cache.Logic
{
    /// <summary>
    /// Handles writing data to existing cache archives, updating the necessary reference tables and checksums.
    /// </summary>
    public class CacheWriter : ICacheWriter
    {
        private readonly IFileStore _store;
        private readonly IReferenceTableProvider _referenceTableProvider;
        private readonly IContainerDecoder _containerFactory;
        private readonly IReferenceTableCodec _referenceTableCodec;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheWriter"/> class.
        /// </summary>
        public CacheWriter(IFileStore store, IReferenceTableProvider referenceTableProvider, IContainerDecoder containerFactory, IReferenceTableCodec referenceTableCodec)
        {
            _store = store;
            _referenceTableProvider = referenceTableProvider;
            _containerFactory = containerFactory;
            _referenceTableCodec = referenceTableCodec;
        }

        /// <summary>
        /// Writes a data container to an existing archive in the cache, automatically updating the version, CRC, and whirlpool digest in the corresponding reference table.
        /// </summary>
        /// <param name="indexId">The identifier of the index (cache) to write to.</param>
        /// <param name="fileId">The identifier of the file to write.</param>
        /// <param name="container">The container holding the data to be written.</param>
        /// <exception cref="IOException">Thrown when attempting to write to the master checksum table or to an archive without an existing reference-table entry.</exception>
        public void Write(int indexId, int fileId, IContainer container)
        {
            ValidateTarget(indexId, fileId);
            Write(indexId, fileId, container, _referenceTableProvider.ReadReferenceTable(indexId));
        }

        /// <inheritdoc />
        public void Write(int indexId, int fileId, IContainer container, int[] xteaKeys)
        {
            ValidateTarget(indexId, fileId);
            ValidateKeys(xteaKeys);
            Write(indexId, fileId, container, _referenceTableProvider.ReadReferenceTable(indexId), xteaKeys);
        }

        /// <inheritdoc />
        public void Write(int indexId, int fileId, IContainer container, IReferenceTable table) => Write(indexId, fileId, container, table, null);

        private void Write(int indexId, int fileId, IContainer container, IReferenceTable table, int[]? xteaKeys)
        {
            ValidateTarget(indexId, fileId);

            /* child member mappings cannot be inferred from a container write */
            var entry = table.GetEntry(fileId);
            if (entry == null)
            {
                throw new IOException($"Cannot write archive {fileId} in index {indexId} without an existing reference-table entry.");
            }

            /* the reference-table version remains authoritative when physical containers omit the version trailer */
            entry.Version++;

            /* grab the bytes we need for the checksum */
            var bytes = container.Encode(false);

            /* protected groups are encrypted after the 5 byte header, and the checksums cover the bytes as stored */
            if (xteaKeys != null)
            {
                XteaEncryptor.EncryptXtea(xteaKeys, bytes, 5, bytes.Length);
            }

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
        private static void ValidateKeys(int[] xteaKeys)
        {
            ArgumentNullException.ThrowIfNull(xteaKeys);
            if (xteaKeys.Length != 4)
            {
                throw new ArgumentException("Exactly four XTEA keys are required.", nameof(xteaKeys));
            }

            /* readers treat all-zero keys as no encryption, so a file encrypted with them could never be read back */
            if (xteaKeys.All(key => key == 0))
            {
                throw new ArgumentException("All-zero XTEA keys do not encrypt.", nameof(xteaKeys));
            }
        }

        private static void ValidateTarget(int indexId, int fileId)
        {
            if (indexId == 255 && fileId == 255)
                throw new IOException("Checksum tables can only be read with the low level FileStore API!");
        }
    }
}
