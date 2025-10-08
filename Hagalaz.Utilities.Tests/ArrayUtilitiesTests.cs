using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class ArrayUtilitiesTests
    {
        [TestMethod]
        public void MakeArray_WithMultipleNonEmptyArrays_ReturnsCombinedArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { 1, 2 }, new[] { 3, 4, 5 }, new[] { 6 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithSomeEmptyArrays_ReturnsCombinedArrayOfNonEmpty()
        {
            var result = ArrayUtilities.MakeArray(new[] { 1, 2 }, new int[0], new[] { 3, 4 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithSingleArray_ReturnsSameArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { 1, 2, 3 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithAllEmptyArrays_ReturnsEmptyArray()
        {
            var result = ArrayUtilities.MakeArray(new int[0], new int[0], new int[0]);
            CollectionAssert.AreEqual(new int[0], result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithSingleEmptyArray_ReturnsEmptyArray()
        {
            var result = ArrayUtilities.MakeArray(new int[0]);
            CollectionAssert.AreEqual(new int[0], result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithNoArguments_ReturnsEmptyArray()
        {
            var result = ArrayUtilities.MakeArray();
            CollectionAssert.AreEqual(new int[0], result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithNegativeNumbers_ReturnsCombinedArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { -1, -2 }, new[] { -3, -4 });
            CollectionAssert.AreEqual(new[] { -1, -2, -3, -4 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithZeros_ReturnsCombinedArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { 0, 0 }, new[] { 1, 0 });
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 0 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithMixedValues_ReturnsCombinedArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { -1, 0, 1 }, new[] { 2, -2 });
            CollectionAssert.AreEqual(new[] { -1, 0, 1, 2, -2 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithJaggedArrays_ReturnsCombinedArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { 1 }, new[] { 2, 3 }, new[] { 4, 5, 6 });
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, result.ToArray());
        }

        [TestMethod]
        public void MakeArray_WithOneArrayAndOneEmpty_ReturnsCorrectArray()
        {
            var result = ArrayUtilities.MakeArray(new[] { 1, 2, 3 }, new int[0]);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.ToArray());
        }
    }
}