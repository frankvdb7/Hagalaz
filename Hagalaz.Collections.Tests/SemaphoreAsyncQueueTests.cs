
namespace Hagalaz.Collections.Tests
{
    [TestClass]
    public class SemaphoreAsyncQueueTests
    {
        [TestMethod]
        public async Task TestEnqueueAndDequeue()
        {
            var queue = new SemaphoreAsyncQueue<int>();
            queue.Enqueue(1);
            var item = await queue.DequeueAsync(default);
            Assert.AreEqual(1, item);
        }
    }
}
