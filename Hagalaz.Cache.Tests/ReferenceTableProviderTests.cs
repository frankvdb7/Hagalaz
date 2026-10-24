using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ReferenceTableProviderTests
    {
        [Fact]
        public void ReadReferenceTable_ShouldReturnReferenceTable()
        {
            // Arrange
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var referenceTableCodec = new ReferenceTableCodec();
            var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 1);
            var entry = new ReferenceTableEntry(0);
            entry.InitializeEntries(1);
            entry.Id = 1;
            entry.Crc32 = 0x12345678;
            entry.Version = 2;
            var childEntry = new ReferenceTableChildEntry(0)
            {
                Id = 3
            };
            entry.AddEntry(0, childEntry);
            table.AddEntry(0, entry);
            var encoded = referenceTableCodec.Encode(table);

            var containerMock = new Mock<IContainer>();
            containerMock.Setup(c => c.Data).Returns(encoded);

            fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(255);
            fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            containerDecoderMock.Setup(cf => cf.Decode(It.IsAny<MemoryStream>())).Returns(containerMock.Object);

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, referenceTableCodec);

            // Act
            var result = provider.ReadReferenceTable(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(table.Protocol, result.Protocol);
            Assert.Equal(table.Version, result.Version);
            Assert.Equal(table.Flags, result.Flags);
        }

        [Fact]
        public void Invalidate_CausesReferenceTableToBeReadAgain()
        {
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var codec = new ReferenceTableCodec();
            var encodedTables = new[] { EncodeTable(codec, 1), EncodeTable(codec, 2) };
            var decodeCount = 0;

            fileStoreMock.SetupGet(store => store.IndexFileCount).Returns(255);
            fileStoreMock.Setup(store => store.Read(255, 16)).Returns(() => new MemoryStream([1]));
            containerDecoderMock.Setup(decoder => decoder.Decode(It.IsAny<MemoryStream>()))
                .Returns(() => new Container(new MemoryStream(encodedTables[decodeCount++])));

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, codec);

            var first = provider.ReadReferenceTable(16);
            var cached = provider.ReadReferenceTable(16);

            provider.Invalidate(16);

            var refreshed = provider.ReadReferenceTable(16);

            Assert.Same(first, cached);
            Assert.NotSame(first, refreshed);
            Assert.Equal(1, first.Version);
            Assert.Equal(2, refreshed.Version);
            Assert.Equal(2, decodeCount);
        }

        [Fact]
        public async Task ReadReferenceTable_ConcurrentColdReadsDecodeOnlyOnceForAnIndex()
        {
            const int workerCount = 8;
            using var startGate = new Barrier(workerCount + 1);
            using var decodeStarted = new ManualResetEventSlim();
            using var releaseDecode = new ManualResetEventSlim();
            using var duplicateRead = new ManualResetEventSlim();
            var readCount = 0;
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var codec = new ReferenceTableCodec();
            var encoded = EncodeTable(codec, 1);

            fileStoreMock.SetupGet(store => store.IndexFileCount).Returns(255);
            fileStoreMock.Setup(store => store.Read(255, 16)).Returns(() =>
            {
                if (Interlocked.Increment(ref readCount) > 1)
                {
                    duplicateRead.Set();
                }

                decodeStarted.Set();
                if (!releaseDecode.Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("The test did not release the first reference-table decode.");
                }

                return new MemoryStream([1]);
            });
            containerDecoderMock.Setup(decoder => decoder.Decode(It.IsAny<MemoryStream>()))
                .Returns(() => new Container(new MemoryStream(encoded)));

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, codec);
            var readers = Enumerable.Range(0, workerCount)
                .Select(_ => Task.Factory.StartNew(
                    () =>
                    {
                        if (!startGate.SignalAndWait(TimeSpan.FromSeconds(10)))
                        {
                            throw new TimeoutException("The test readers did not reach the start gate.");
                        }

                        return provider.ReadReferenceTable(16);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default))
                .ToArray();

            var allReadersReady = startGate.SignalAndWait(TimeSpan.FromSeconds(10));
            var firstDecodeStarted = decodeStarted.Wait(TimeSpan.FromSeconds(10));
            var duplicateReadObserved = duplicateRead.Wait(TimeSpan.FromMilliseconds(200));
            releaseDecode.Set();
            var results = await Task.WhenAll(readers);

            Assert.True(allReadersReady);
            Assert.True(firstDecodeStarted);
            Assert.False(duplicateReadObserved);
            Assert.Equal(1, readCount);
            Assert.All(results, result => Assert.Same(results[0], result));
        }

        [Fact]
        public async Task Invalidate_WaitsForColdInitializationAndClearsPublishedTable()
        {
            using var readStarted = new ManualResetEventSlim();
            using var releaseRead = new ManualResetEventSlim();
            using var invalidateStarted = new ManualResetEventSlim();
            var readCount = 0;
            var fileStoreMock = new Mock<IFileStore>();
            var containerDecoderMock = new Mock<IContainerDecoder>();
            var codec = new ReferenceTableCodec();
            var encodedTables = new[] { EncodeTable(codec, 1), EncodeTable(codec, 2) };

            fileStoreMock.SetupGet(store => store.IndexFileCount).Returns(255);
            fileStoreMock.Setup(store => store.Read(255, 16)).Returns(() =>
            {
                var currentRead = Interlocked.Increment(ref readCount);
                if (currentRead == 1)
                {
                    readStarted.Set();
                    if (!releaseRead.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release the initial reference-table read.");
                    }
                }

                return new MemoryStream([1]);
            });
            containerDecoderMock.Setup(decoder => decoder.Decode(It.IsAny<MemoryStream>()))
                .Returns(() => new Container(new MemoryStream(encodedTables[Volatile.Read(ref readCount) - 1])));

            var provider = new ReferenceTableProvider(fileStoreMock.Object, containerDecoderMock.Object, codec);
            var firstRead = Task.Factory.StartNew(
                () => provider.ReadReferenceTable(16),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            Task invalidation = Task.CompletedTask;
            var invalidationCompletedDuringRead = false;
            try
            {
                Assert.True(readStarted.Wait(TimeSpan.FromSeconds(10)));

                invalidation = Task.Factory.StartNew(
                    () =>
                    {
                        invalidateStarted.Set();
                        provider.Invalidate(16);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                Assert.True(invalidateStarted.Wait(TimeSpan.FromSeconds(10)));
                var completedBeforeRead = await Task.WhenAny(invalidation, Task.Delay(TimeSpan.FromMilliseconds(200)));
                invalidationCompletedDuringRead = completedBeforeRead == invalidation;
            }
            finally
            {
                releaseRead.Set();
            }

            var initialTable = await firstRead;
            await invalidation;
            var refreshedTable = provider.ReadReferenceTable(16);

            Assert.False(invalidationCompletedDuringRead);
            Assert.NotSame(initialTable, refreshedTable);
            Assert.Equal(1, initialTable.Version);
            Assert.Equal(2, refreshedTable.Version);
            Assert.Equal(2, readCount);
        }

        private static byte[] EncodeTable(ReferenceTableCodec codec, int version)
        {
            var table = new ReferenceTable(7, version, (ReferenceTableFlags)0, 0);
            using var encoded = codec.Encode(table);
            return encoded.ToArray();
        }
    }
}
