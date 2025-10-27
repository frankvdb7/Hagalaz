using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class StringUtilitiesTests
    {
        [TestMethod]
        [DataRow("hello", 15263440L)]
        [DataRow("world", 43890588L)]
        [DataRow("csharp", 244048205L)]
        [DataRow("programming", 79330059267400463L)]
        [DataRow("a", 1L)]
        [DataRow(" ", 0L)]
        [DataRow("123456789012", 1000000000000000000L)]
        [DataRow("abcdefghijkl", 65535L)]
        [DataRow("____", 0L)]
        [DataRow("999999999999", 2383999999999999999L)]
        [DataRow("____________", 0L)]
        public void StringToLong_ConvertsCorrectly(string s, long expected)
        {
            // Arrange
            // Act
            long actual = StringUtilities.StringToLong(s);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(15263440L, "hello")]
        [DataRow(43890588L, "world")]
        [DataRow(244048205L, "csharp")]
        [DataRow(79330059267400463L, "programming")]
        [DataRow(1L, "a")]
        [DataRow(0L, null)]
        [DataRow(-1L, null)]
        [DataRow(65535L, "aj5h")]
        [DataRow(1000000000000000000L, "123456789012")]
        [DataRow(long.MaxValue, "1y2p4a5a5y1y2p4a5")]
        [DataRow(37L, "aa")]
        [DataRow(1L, "a")]
        [DataRow(36L, "9")]
        public void LongToString_ConvertsCorrectly(long value, string expected)
        {
            // Arrange
            // Act
            string actual = StringUtilities.LongToString(value);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("test@example.com", true)]
        [DataRow("test.name@example.co.uk", true)]
        [DataRow("test", false)]
        [DataRow("test@.com", false)]
        [DataRow("test@example", false)]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("test@example.co.uk", true)]
        [DataRow("test+alias@example.com", true)]
        [DataRow("test@example.museum", true)]
        [DataRow("test@192.168.1.1", false)]
        [DataRow("test@sub.domain.com", true)]
        [DataRow("test@domain.io", true)]
        [DataRow("test@domain..com", false)]
        [DataRow("test@.com", false)]
        [DataRow("test@", false)]
        public void IsValidEmail_ValidatesCorrectly(string email, bool expected)
        {
            // Arrange
            // Act
            bool actual = StringUtilities.IsValidEmail(email);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow("somename", true)]
        [DataRow("some name", true)]
        [DataRow("some-name", true)]
        [DataRow("toolongname123", false)]
        [DataRow("invalid#name", false)]
        [DataRow("", false)]
        [DataRow("a", true)]
        [DataRow("123456789012", true)]
        [DataRow(" name", false)]
        [DataRow("name ", false)]
        [DataRow("some--name", false)]
        [DataRow("some  name", false)]
        public void IsValidName_ValidatesCorrectly(string name, bool expected)
        {
            // Arrange
            // Act
            bool actual = StringUtilities.IsValidName(name);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeValues_IntArray_EncodesCorrectly()
        {
            // Arrange
            var values = new int[] { 1, 2, 3 };
            var expected = "1,2,3";

            // Act
            var actual = StringUtilities.EncodeValues(values);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeValues_StringArray_EncodesCorrectly()
        {
            // Arrange
            var values = new string[] { "a", "b", "c" };
            var expected = "a,b,c";

            // Act
            var actual = StringUtilities.EncodeValues(values);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeValues_EmptyArray_ReturnsEmptyString()
        {
            // Arrange
            var values = new int[] { };
            var expected = "";

            // Act
            var actual = StringUtilities.EncodeValues(values);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EncodeValues_BoolArray_EncodesCorrectly()
        {
            // Arrange
            var values = new bool[] { true, false, true };
            var expected = "1,0,1";

            // Act
            var actual = StringUtilities.EncodeValues(values);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectDoubleFromString_ValidInput_ReturnsCorrectDoubles()
        {
            // Arrange
            var input = "1.1,2.2,3.3";
            var expected = new double[] { 1.1, 2.2, 3.3 };

            // Act
            var actual = StringUtilities.SelectDoubleFromString(input).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectDoubleFromString_InvalidInput_ReturnsZeroForInvalid()
        {
            // Arrange
            var input = "1.1,abc,3.3";
            var expected = new double[] { 1.1, 0.0, 3.3 };

            // Act
            var actual = StringUtilities.SelectDoubleFromString(input).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectDoubleFromString_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var actual = StringUtilities.SelectDoubleFromString(input).ToArray();

            // Assert
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        public void SelectIntFromString_ValidInput_ReturnsCorrectInts()
        {
            // Arrange
            var input = "1,2,3";
            var expected = new int[] { 1, 2, 3 };

            // Act
            var actual = StringUtilities.SelectIntFromString(input).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectBoolFromString_ValidInput_ReturnsCorrectBools()
        {
            // Arrange
            var input = "1,0,1";
            var expected = new bool[] { true, false, true };

            // Act
            var actual = StringUtilities.SelectBoolFromString(input).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DecodeValues_String_DecodesCorrectly()
        {
            // Arrange
            var data = "1,2,3";
            var expected = new int[] { 1, 2, 3 };

            // Act
            var actual = StringUtilities.DecodeValues(data, int.Parse);

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DecodeValues_StringWithCustomSeparator_DecodesCorrectly()
        {
            // Arrange
            var data = "1;2;3";
            var expected = new int[] { 1, 2, 3 };

            // Act
            var actual = StringUtilities.DecodeValues(data, int.Parse, ';');

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DecodeValues_EmptyString_ReturnsEmptyArray()
        {
            // Arrange
            var data = "";

            // Act
            var actual = StringUtilities.DecodeValues(data, int.Parse);

            // Assert
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        public void DecodeValues_Bool_DecodesCorrectly()
        {
            // Arrange
            var data = "1,0,1";
            var expected = new bool[] { true, false, true };

            // Act
            var actual = StringUtilities.DecodeValues(data);

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetStringInBetween_FindsString()
        {
            // Arrange
            var source = "begin THE_STRING end";
            var expected = "THE_STRING";

            // Act
            var result = StringUtilities.GetStringInBetween("begin ", " end", source, false, false);

            // Assert
            Assert.AreEqual(expected, result[0]);
            Assert.AreEqual("", result[1]);
        }

        [TestMethod]
        public void FormatNumber_FormatsCorrectly()
        {
            // Arrange
            var value = 1234567;
            var expected = "1,234,567";

            // Act
            var actual = StringUtilities.FormatNumber(value);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectIntFromString_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var actual = StringUtilities.SelectIntFromString(input).ToArray();

            // Assert
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        public void SelectIntFromString_InvalidInput_ReturnsZeroForInvalid()
        {
            // Arrange
            var input = "1,abc,3";
            var expected = new int[] { 1, 0, 3 };

            // Act
            var actual = StringUtilities.SelectIntFromString(input).ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectBoolFromString_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var actual = StringUtilities.SelectBoolFromString(input).ToArray();

            // Assert
            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        [DataRow("HELLO", 15263440L)]
        public void StringToLong_WithUppercase_ConvertsCorrectly(string s, long expected)
        {
            // Arrange
            // Act
            long actual = StringUtilities.StringToLong(s);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetStringInBetween_IncludesBeginAndEnd()
        {
            // Arrange
            var source = "begin THE_STRING end";
            var expected = "begin THE_STRING end";

            // Act
            var result = StringUtilities.GetStringInBetween("begin ", " end", source, true, true);

            // Assert
            Assert.AreEqual(expected, result[0]);
        }

        [TestMethod]
        [DataRow(0, "0")]
        [DataRow(123, "123")]
        [DataRow(1234, "1,234")]
        [DataRow(1234567, "1,234,567")]
        [DataRow(-1234567, "-1,234,567")]
        public void FormatNumber_VariousInputs_FormatsCorrectly(int value, string expected)
        {
            // Act
            var actual = StringUtilities.FormatNumber(value);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}