using Hagalaz.Game.Scripts.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Hagalaz.Game.Scripts.Tests
{
    [TestClass]
    public class ServerTextWriterTests
    {
        [TestMethod]
        public void Write_AppendsToString()
        {
            string output = "";
            var writer = new ServerTextWriter(s => output = s);
            writer.Write("Hello");
            Assert.AreEqual("", output); // Not flushed yet
        }

        [TestMethod]
        public void WriteLine_FlushesToString()
        {
            string output = "";
            var writer = new ServerTextWriter(s => output = s);
            writer.WriteLine("Hello");
            Assert.AreEqual("Hello", output);
        }

        [TestMethod]
        public void WriteLine_NullValue_FlushesEmpty()
        {
            string output = "initial";
            var writer = new ServerTextWriter(s => output = s);
            writer.WriteLine((string?)null);
            Assert.AreEqual("", output);
        }
    }
}
