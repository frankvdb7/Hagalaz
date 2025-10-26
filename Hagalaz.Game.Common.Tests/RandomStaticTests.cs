using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Hagalaz.Game.Common.Tests
{
    [TestClass]
    public class RandomStaticTests
    {
        [TestMethod]
        public void Next_WithoutMaxValue_ShouldReturnNonNegative()
        {
            var result = RandomStatic.Generator.Next();
            Assert.IsTrue(result >= 0);
        }

        [TestMethod]
        public void Next_WithMaxValue_ShouldReturnInRange()
        {
            var maxValue = 100;
            var result = RandomStatic.Generator.Next(maxValue);
            Assert.IsTrue(result >= 0 && result < maxValue);
        }

        [TestMethod]
        public void NextDouble_ShouldReturnInRange()
        {
            var result = RandomStatic.Generator.NextDouble();
            Assert.IsTrue(result >= 0.0 && result < 1.0);
        }

        [TestMethod]
        public void NextBytes_ShouldFillArray()
        {
            var buffer = new byte[10];
            RandomStatic.Generator.NextBytes(buffer);
            foreach (var b in buffer)
            {
                if (b != 0)
                {
                    Assert.IsTrue(true);
                    return;
                }
            }
            Assert.Fail("The buffer was not filled with random bytes.");
        }

        [TestMethod]
        public void ThreadSafety_ShouldNotThrowExceptions()
        {
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        RandomStatic.Generator.Next();
                        RandomStatic.Generator.Next(100);
                        RandomStatic.Generator.NextDouble();
                        var buffer = new byte[10];
                        RandomStatic.Generator.NextBytes(buffer);
                    }
                });
            }

            Task.WaitAll(tasks);
        }
    }
}
