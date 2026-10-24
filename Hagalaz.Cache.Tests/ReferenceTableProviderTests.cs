using System.IO;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ReferenceTableProviderTests
{
    [Fact]
    public void ReadReferenceTable_DecodesASeparateTableForEveryCall()
    {
        var store = new Mock<IFileStore>();
        var containerDecoder = new Mock<IContainerDecoder>();
        var codec = new ReferenceTableCodec();
        var encodedTables = new[] { EncodeTable(codec, 1), EncodeTable(codec, 2) };
        var decodeCount = 0;

        store.SetupGet(current => current.IndexFileCount).Returns(255);
        store.Setup(current => current.Read(255, 16)).Returns(() => new MemoryStream([1]));
        containerDecoder.Setup(current => current.Decode(It.IsAny<MemoryStream>()))
            .Returns(() => new Container(new MemoryStream(encodedTables[Interlocked.Increment(ref decodeCount) - 1])));

        var provider = new ReferenceTableProvider(store.Object, containerDecoder.Object, codec);

        var first = provider.ReadReferenceTable(16);
        var second = provider.ReadReferenceTable(16);

        Assert.NotSame(first, second);
        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
        Assert.NotNull(first.GetEntry(1));
        Assert.NotNull(second.GetEntry(2));
        Assert.Equal(21, first.GetEntry(1, 1)!.Id);
        Assert.Equal(22, second.GetEntry(2, 2)!.Id);
        Assert.Equal(2, decodeCount);
        store.Verify(current => current.Read(255, 16), Times.Exactly(2));
    }

    [Fact]
    public void ReadReferenceTable_DisposesStoreStreamAndDecodedContainer()
    {
        var fileBuffer = new MemoryStreamSpy();
        fileBuffer.WriteByte(1);
        fileBuffer.Position = 0;
        using var containerData = new MemoryStream(EncodeTable(new ReferenceTableCodec(), 7));
        var container = new Mock<IContainer>();
        container.SetupGet(current => current.Data).Returns(containerData);
        var store = new Mock<IFileStore>();
        store.SetupGet(current => current.IndexFileCount).Returns(255);
        store.Setup(current => current.Read(255, 16)).Returns(fileBuffer);
        var containerDecoder = new Mock<IContainerDecoder>();
        containerDecoder.Setup(current => current.Decode(fileBuffer)).Returns(container.Object);

        var provider = new ReferenceTableProvider(store.Object, containerDecoder.Object, new ReferenceTableCodec());

        var result = provider.ReadReferenceTable(16);

        Assert.Equal(7, result.Version);
        Assert.True(fileBuffer.IsDisposed);
        container.Verify(current => current.Dispose(), Times.Once);
    }

    [Fact]
    public void ReadReferenceTable_PropagatesCorruptDataAndDisposesTemporaryResources()
    {
        var fileBuffer = new MemoryStreamSpy();
        fileBuffer.WriteByte(1);
        fileBuffer.Position = 0;
        using var containerData = new MemoryStream([1]);
        var container = new Mock<IContainer>();
        container.SetupGet(current => current.Data).Returns(containerData);
        var store = new Mock<IFileStore>();
        store.SetupGet(current => current.IndexFileCount).Returns(255);
        store.Setup(current => current.Read(255, 16)).Returns(fileBuffer);
        var containerDecoder = new Mock<IContainerDecoder>();
        containerDecoder.Setup(current => current.Decode(fileBuffer)).Returns(container.Object);
        var failure = new InvalidDataException("corrupt reference table");
        var codec = new Mock<IReferenceTableCodec>();
        codec.Setup(current => current.Decode(containerData, true)).Throws(failure);
        var provider = new ReferenceTableProvider(store.Object, containerDecoder.Object, codec.Object);

        var actual = Assert.Throws<InvalidDataException>(() => provider.ReadReferenceTable(16));

        Assert.Same(failure, actual);
        Assert.True(fileBuffer.IsDisposed);
        container.Verify(current => current.Dispose(), Times.Once);
    }

    [Fact]
    public void ReadReferenceTable_PreservesInvalidAndReservedIndexFailures()
    {
        var store = new Mock<IFileStore>();
        store.SetupGet(current => current.IndexFileCount).Returns(256);
        var provider = new ReferenceTableProvider(
            store.Object,
            new Mock<IContainerDecoder>().Object,
            new Mock<IReferenceTableCodec>().Object);

        Assert.Throws<IndexOutOfRangeException>(() => provider.ReadReferenceTable(-1));
        Assert.Throws<FileNotFoundException>(() => provider.ReadReferenceTable(256));
        Assert.Throws<IOException>(() => provider.ReadReferenceTable(255));
        store.Verify(current => current.Read(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    private static byte[] EncodeTable(ReferenceTableCodec codec, int version)
    {
        var table = new ReferenceTable(7, version, ReferenceTableFlags.Identifiers, 1);
        var entry = new ReferenceTableEntry(version);
        entry.InitializeEntries(1);
        entry.AddEntry(version, new ReferenceTableChildEntry(0) { Id = version + 20 });
        table.AddEntry(version, entry);
        using var encoded = codec.Encode(table);
        return encoded.ToArray();
    }
}
