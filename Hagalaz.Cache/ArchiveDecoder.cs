using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache
{
    public class ArchiveDecoder : IArchiveDecoder
    {
        public Archive Decode(IContainer container, int size)
        {
            var stream = container.Data;
            var archive = new Archive(size);

            if (size <= 1)
            {
                archive.Entries = new MemoryStream[1];
                archive.Entries[0] = new MemoryStream(stream.ToArray());
                return archive;
            }

            stream.Position = stream.Length - 1;
            var chunks = stream.ReadUnsignedByte();

            var chunkSizes = new int[chunks, size];
            stream.Position = stream.Length - 1 - chunks * size * 4;

            for (var chunk = 0; chunk < chunks; chunk++)
            {
                var cumulativeChunkSize = 0;
                for (var id = 0; id < size; id++)
                {
                    cumulativeChunkSize += stream.ReadInt();
                    chunkSizes[chunk, id] = cumulativeChunkSize;
                }
            }

            var fileData = new MemoryStream[size];
            for(int i = 0; i < size; i++)
            {
                fileData[i] = new MemoryStream();
            }

            stream.Position = 0;
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                var lastChunkSize = 0;
                for (var id = 0; id < size; id++)
                {
                    var currentChunkSize = chunkSizes[chunk, id];
                    var delta = currentChunkSize - lastChunkSize;
                    lastChunkSize = currentChunkSize;

                    var temp = new byte[delta];
                    stream.Read(temp, 0, delta);
                    fileData[id].Write(temp, 0, delta);
                }
            }

            archive.Entries = new MemoryStream[size];
            for(int i = 0; i < size; i++)
            {
                archive.Entries[i] = new MemoryStream(fileData[i].ToArray());
            }

            return archive;
        }
    }
}
