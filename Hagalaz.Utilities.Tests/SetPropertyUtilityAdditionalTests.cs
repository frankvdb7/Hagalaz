using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class SetPropertyUtilityAdditionalTests
    {
        private class TestClass
        {
            public string Value { get; set; }
        }

        private struct TestStruct
        {
            public int Value { get; set; }

            public override bool Equals(object obj)
            {
                return obj is TestStruct other && Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }

        [TestMethod]
        public void SetClass_WhenBothNull_ShouldReturnFalse()
        {
            TestClass obj = null;
            TestClass newValue = null;
            Assert.IsFalse(SetPropertyUtility.SetClass(ref obj, newValue));
            Assert.IsNull(obj);
        }

        [TestMethod]
        public void SetStruct_WithEquatableStruct_ShouldUseEquals()
        {
            var s1 = new TestStruct { Value = 1 };
            var s2 = new TestStruct { Value = 1 };
            Assert.IsFalse(SetPropertyUtility.SetStruct(ref s1, s2));
        }
    }
}
