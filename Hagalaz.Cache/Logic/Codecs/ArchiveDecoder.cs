using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Models;

namespace Hagalaz.Cache.Logic.Codecs
{
    /// <summary>
    /// Decodes an <see cref="Archive"/> from a data container. An archive is a file within the cache
    /// that can have multiple member files inside it, often used for grouping related data.
    /// </summary>
    public class ArchiveDecoder : IArchiveDecoder
    {
        /// <summary>
        /// Decodes the archive data from the provided container.
        /// </summary>
        /// <param name="container">The container holding the raw, compressed archive data.</param>
        /// <param name="size">The number of member file entries expected in the archive.</param>
        /// <returns>A decoded <see cref="Archive"/> with its member file entries populated.</returns>
        public IArchive Decode(IContainer container, int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "An archive must contain at least one entry.");
            }

            var stream = container.Data;
            var archive = new Archive(size);

            if (size <= 1)
            {
                archive.Entries = new MemoryStream[1];
                archive.Entries[0] = new MemoryStream(stream.ToArray());
                return archive;
            }

            if (stream.Length < 1)
            {
                throw new InvalidDataException("Archive is missing its chunk count.");
            }

            stream.Position = stream.Length - 1;
            var chunks = stream.ReadUnsignedByte();
            if (chunks == 0)
            {
                throw new InvalidDataException("Archive must contain at least one data chunk.");
            }

            var footerSize = (long)chunks * size * sizeof(int);
            var footerStart = stream.Length - 1 - footerSize;
            if (footerStart < 0 || footerSize > int.MaxValue)
            {
                throw new InvalidDataException("Archive footer is outside the archive data.");
            }

            var chunkSizes = new int[chunks, size];
            stream.Position = footerStart;

            for (var chunk = 0; chunk < chunks; chunk++)
            {
                var cumulativeChunkSize = 0;
                for (var id = 0; id < size; id++)
                {
                    try
                    {
                        cumulativeChunkSize = checked(cumulativeChunkSize + stream.ReadInt());
                    }
                    catch (OverflowException exception)
                    {
                        throw new InvalidDataException("Archive footer contains an overflowing chunk size.", exception);
                    }

                    if (cumulativeChunkSize < 0)
                    {
                        throw new InvalidDataException("Archive footer contains a negative chunk size.");
                    }

                    chunkSizes[chunk, id] = cumulativeChunkSize;
                }
            }

            var fileSizes = new long[size];
            long dataSize = 0;
            for (var id = 0; id < size; id++)
            {
                for (var chunk = 0; chunk < chunks; chunk++)
                {
                    fileSizes[id] += chunkSizes[chunk, id];
                    dataSize += chunkSizes[chunk, id];
                }

                if (fileSizes[id] > int.MaxValue)
                {
                    throw new InvalidDataException("Archive member is too large.");
                }
            }

            if (dataSize != footerStart)
            {
                throw new InvalidDataException("Archive footer sizes do not match the archive data.");
            }

            for (var id = 0; id < size; id++)
            {
                archive.Entries![id] = new MemoryStream((int)fileSizes[id]);
            }

            stream.Position = 0;
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                for (var id = 0; id < size; id++)
                {
                    var currentChunkSize = chunkSizes[chunk, id];

                    var temp = new byte[currentChunkSize];
                    stream.ReadExactly(temp);
                    archive.Entries![id].Write(temp, 0, currentChunkSize);
                }
            }

            for (var id = 0; id < size; id++)
            {
                archive.Entries![id].Position = 0;
            }

            return archive;
        }
    }
}
