using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ReferenceTableCacheTests
{
    [Fact]
    public void Get_MemoizesPerIndexAndClearOnlyClearsTheRequestedTable()
    {
        var firstTable = new Mock<IReferenceTable>().Object;
        var refreshedTable = new Mock<IReferenceTable>().Object;
        var unrelatedTable = new Mock<IReferenceTable>().Object;
        var provider = new Mock<IReferenceTableProvider>();
        provider.SetupSequence(current => current.ReadReferenceTable(0))
            .Returns(firstTable)
            .Returns(refreshedTable);
        provider.Setup(current => current.ReadReferenceTable(1)).Returns(unrelatedTable);
        var cache = new ReferenceTableCache(provider.Object, 3);

        Assert.Same(firstTable, cache.Get(0));
        Assert.Same(firstTable, cache.Get(0));
        Assert.Same(unrelatedTable, cache.Get(1));

        cache.Clear(0);
        cache.Clear(255);
        cache.Clear(3);

        Assert.Same(refreshedTable, cache.Get(0));
        Assert.Same(unrelatedTable, cache.Get(1));
        provider.Verify(current => current.ReadReferenceTable(0), Times.Exactly(2));
        provider.Verify(current => current.ReadReferenceTable(1), Times.Once);
    }

    [Fact]
    public void Get_PreservesInvalidIndexFailures()
    {
        var provider = new Mock<IReferenceTableProvider>();
        var cache = new ReferenceTableCache(provider.Object, 3);

        Assert.Throws<IndexOutOfRangeException>(() => cache.Get(-1));
        Assert.Throws<FileNotFoundException>(() => cache.Get(3));
        provider.Verify(current => current.ReadReferenceTable(It.IsAny<int>()), Times.Never);
    }
}
