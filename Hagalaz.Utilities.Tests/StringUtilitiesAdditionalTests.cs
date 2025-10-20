using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class StringUtilitiesAdditionalTests
    {
        [TestMethod]
        public void EncodeValues_WithDifferentSeparators_EncodesCorrectly()
        {
            var values = new int[] { 1, 2, 3 };
            Assert.AreEqual("1;2;3", StringUtilities.EncodeValues(values, ';'));
            Assert.AreEqual("1-2-3", StringUtilities.EncodeValues(values, '-'));
        }

        [TestMethod]
        public void DecodeValues_WithDifferentSeparators_DecodesCorrectly()
        {
            var data = "1;2;3";
            var expected = new int[] { 1, 2, 3 };
            var actual = StringUtilities.DecodeValues(data, int.Parse, ';');
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetStringInBetween_MultipleOccurrences_FindsAll()
        {
            var source = "begin THE_STRING end begin ANOTHER_STRING end";
            var result = StringUtilities.GetStringInBetween("begin ", " end", source, true, false);
            Assert.AreEqual("THE_STRING", result[0]);
            Assert.AreEqual("ANOTHER_STRING", result[1]);
        }

        [TestMethod]
        public void GetStringInBetween_Greedy_FindsLongest()
        {
            var source = "begin THE_STRING end begin ANOTHER_STRING end";
            var expected = "THE_STRING end begin ANOTHER_STRING";
            var result = StringUtilities.GetStringInBetween("begin ", " end", source, false, true);
            Assert.AreEqual(expected, result[0]);
        }
    }
}
