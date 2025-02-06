using System.Text;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class HashHelperTests
    {
        private const string PlainText = "Stay hungry and stay foolish";

        /*
         * MD5 yields hexadecimal digits (0-15 / 0-F), so they are four bits each. 128 / 4 = 32 characters.
         */
        [TestMethod]
        public void MD5()
        {
            var result1 = HashHelper.ComputeHash(PlainText, HashType.MD5);

            Assert.IsTrue(Encoding.UTF8.GetByteCount(result1) == 32);

            Assert.IsTrue(result1.ToCharArray().All(c => char.IsLower(c) || char.IsDigit(c)));
        }

        /**
         * SHA-1 yields hexadecimal digits too (0-15 / 0-F), so 160 / 4 = 40 characters.
         */
        [TestMethod]
        public void SHA1()
        {
            var result1 = HashHelper.ComputeHash(PlainText, HashType.SHA1);

            Assert.IsTrue(Encoding.UTF8.GetByteCount(result1) == 40);

            Assert.IsTrue(result1.ToCharArray().All(c => char.IsLower(c) || char.IsDigit(c)));
        }

        /**
         * SHA-2 (256 variant) yields hexadecimal digits too (0-15 / 0-F), so 256 / 4 = 64 characters.
         */
        [TestMethod]
        public void SHA256()
        {
            var result1 = HashHelper.ComputeHash(PlainText, HashType.SHA256);

            Assert.IsTrue(Encoding.UTF8.GetByteCount(result1) == 64);


            Assert.IsTrue(result1.ToCharArray().All(c => char.IsLower(c) || char.IsDigit(c)));
        }


        /**
         * SHA-2 (384 variant) yields hexadecimal digits too (0-15 / 0-F), so 384 / 4 = 96 characters.
         */
        [TestMethod]
        public void SHA384()
        {
            var result1 = HashHelper.ComputeHash(PlainText, HashType.SHA384);
            Assert.IsTrue(Encoding.UTF8.GetByteCount(result1) == 96);

            Assert.IsTrue(result1.ToCharArray().All(c => char.IsLower(c) || char.IsDigit(c)));
        }

        /**
        * SHA-2 (512 variant) yields hexadecimal digits too (0-15 / 0-F), so 512 / 4 = 128 characters.
       */
        [TestMethod]
        public void SHA512()
        {
            var result1 = HashHelper.ComputeHash(PlainText, HashType.SHA512);

            Assert.IsTrue(Encoding.UTF8.GetByteCount(result1) == 128);

            Assert.IsTrue(result1.ToCharArray().All(c => char.IsLower(c) || char.IsDigit(c)));
        }
    }
}