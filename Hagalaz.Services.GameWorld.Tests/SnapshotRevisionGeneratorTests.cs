using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class SnapshotRevisionGeneratorTests
{
    [TestMethod]
    public void Next_ReturnsIncreasingRevisions()
    {
        var generator = new SnapshotRevisionGenerator();

        var first = generator.Next();
        var second = generator.Next();

        Assert.IsTrue(second > first);
    }

    [TestMethod]
    public async Task Next_ConcurrentCalls_ReturnUniqueRevisions()
    {
        var generator = new SnapshotRevisionGenerator();

        var revisions = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => Task.Run(generator.Next)));

        Assert.AreEqual(revisions.Length, revisions.Distinct().Count());
    }
}
