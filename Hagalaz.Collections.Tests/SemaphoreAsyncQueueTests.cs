using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Collections;
using System.Threading.Tasks;

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
