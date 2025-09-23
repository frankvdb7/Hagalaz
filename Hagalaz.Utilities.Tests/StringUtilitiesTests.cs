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
        public void IsValidName_ValidatesCorrectly(string name, bool expected)
        {
            // Arrange
            // Act
            bool actual = StringUtilities.IsValidName(name);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
