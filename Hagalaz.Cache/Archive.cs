using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    /// <summary>
    /// An <see cref="Archive"/> is a file within the cache that can have multiple member
    /// files inside it.
    /// </summary>
    public class Archive : IDisposable
    {
        /// <summary>
        /// The array of entries in this archive.
        /// </summary>
        private MemoryStream[]? _entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="Archive" /> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public Archive(int size) => _entries = new MemoryStream[size];

        /// <summary>
        /// Decodes the specified <see cref="MemoryStream" /> into an <see cref="Archive" />
        /// </summary>
        /// <param name="container">The container containing the archive.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public static Archive Decode(Container container, int size)
        {
            var stream = container.Data;

            /* allocate a new archive object */
            var archive = new Archive(size);

            /* only has 1 file, just return the uncompressed data */
            if (size <= 1)
            {
                archive._entries = new MemoryStream[1];
                archive._entries[0] = new MemoryStream(stream.ToArray());
                return archive;
            }

            /* read the number of chunks at the end of the archive */
            stream.Position = stream.Length - 1;
            int chunks = stream.ReadUnsignedByte();

            /* read the sizes of the child entries and individual chunks */
            int[,] chunkSizes = new int[chunks, size];
            int[] sizes = new int[size];
            stream.Position = stream.Length - 1 - chunks * (size * 4);
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                int chunkSize = 0;
                for (var id = 0; id < size; id++)
                {
                    /* read the delta-encoded chunk length */
                    chunkSize += stream.ReadInt();

                    chunkSizes[chunk, id] = chunkSize; /* store the size of this chunk */
                    sizes[id] += chunkSize; /* and add it to the size of the whole file */
                }
            }

            /* allocate the buffers for the child entries */
            for (var id = 0; id < size; id++)
            {
                archive._entries![id] = new MemoryStream(sizes[id]);
            }

            /* read the data into the buffers */
            stream.Position = 0;
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                for (var id = 0; id < size; id++)
                {
                    /* get the length of this chunk */
                    int chunkSize = chunkSizes[chunk, id];

                    /* copy this chunk into a temporary buffer */
                    byte[] temp = new byte[chunkSize];
                    stream.Read(temp, 0, chunkSize);

                    /* copy the temporary buffer into the file buffer */
                    archive._entries![id].Write(temp, 0, chunkSize);
                }
            }

            /* flip all of the buffers */
            for (var id = 0; id < size; id++)
            {
                archive._entries![id].SetLength(archive._entries[id].Position);
                archive._entries[id].Position = 0;
            }

            return archive;
        }

        /// <summary>
        /// Gets the entry.
        /// </summary>
        /// <param name="subFileId">The sub file identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public MemoryStream GetEntry(int subFileId)
        {
            if (_entries == null)
            {
                throw new InvalidOperationException($"{nameof(Archive)} is not decoded");
            }
            if (subFileId < 0 || subFileId >= _entries.Length)
                throw new ArgumentOutOfRangeException();
            return _entries[subFileId];
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing || _entries == null)
            {
                return;
            }
            
            foreach (var t in _entries)
            {
                t.Dispose();
            }

            _entries = null;
        }
    }
}
