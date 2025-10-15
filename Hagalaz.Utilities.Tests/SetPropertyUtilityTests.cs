using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class SetPropertyUtilityTests
    {
        private class TestClass
        {
            public string Value { get; set; }

            public override bool Equals(object obj)
            {
                return obj is TestClass other && Value == other.Value;
            }

            public override int GetHashCode()
            {
                return Value?.GetHashCode() ?? 0;
            }
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
        public void SetClass_WhenValueChanged_ReturnsTrue()
        {
            var oldValue = new TestClass { Value = "a" };
            var newValue = new TestClass { Value = "b" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.IsTrue(result);
            Assert.AreEqual(newValue, oldValue);
        }

        [TestMethod]
        public void SetClass_WhenValueChangesFromNull_ReturnsTrue()
        {
            TestClass? oldValue = null;
            var newValue = new TestClass { Value = "a" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.IsTrue(result);
            Assert.AreEqual(newValue, oldValue);
        }

        [TestMethod]
        public void SetClass_WhenValueChangesToNull_ReturnsTrue()
        {
            var oldValue = new TestClass { Value = "a" };
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.IsTrue(result);
            Assert.IsNull(oldValue);
        }

        [TestMethod]
        public void SetClass_WhenValuesAreSame_ReturnsFalse()
        {
            var oldValue = new TestClass { Value = "a" };
            var newValue = new TestClass { Value = "a" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.IsFalse(result);
            Assert.AreEqual(newValue, oldValue);
        }

        [TestMethod]
        public void SetClass_WhenBothValuesAreNull_ReturnsFalse()
        {
            TestClass? oldValue = null;
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.IsFalse(result);
            Assert.IsNull(oldValue);
        }

        [TestMethod]
        public void SetStruct_WhenValueChanged_ReturnsTrue()
        {
            var oldValue = new TestStruct { Value = 1 };
            var newValue = new TestStruct { Value = 2 };

            var result = SetPropertyUtility.SetStruct(ref oldValue, newValue);

            Assert.IsTrue(result);
            Assert.AreEqual(newValue, oldValue);
        }

        [TestMethod]
        public void SetStruct_WhenValuesAreSame_ReturnsFalse()
        {
            var oldValue = new TestStruct { Value = 1 };
            var newValue = new TestStruct { Value = 1 };

            var result = SetPropertyUtility.SetStruct(ref oldValue, newValue);

            Assert.IsFalse(result);
            Assert.AreEqual(newValue, oldValue);
        }
    }
}
