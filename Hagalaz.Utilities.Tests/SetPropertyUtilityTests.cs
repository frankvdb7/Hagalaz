using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Utilities.Tests
{
    [TestClass]
    public class SetPropertyUtilityTests
    {
        private class TestClass(int value)
        {
            public int Value { get; set; } = value;

            public override bool Equals(object? obj)
            {
                if (obj is TestClass other)
                {
                    return Value == other.Value;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }

        private struct TestStruct(int value)
        {
            public int Value { get; set; } = value;
        }

        #region SetClass
        [TestMethod]
        public void SetClass_ShouldReturnFalse_WhenBothValuesAreNull()
        {
            TestClass? currentValue = null;
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsFalse(result);
            Assert.IsNull(currentValue);
        }

        [TestMethod]
        public void SetClass_ShouldReturnTrue_WhenCurrentValueIsNullAndNewValueIsNotNull()
        {
            TestClass? currentValue = null;
            var newValue = new TestClass(1);

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsTrue(result);
            Assert.AreSame(newValue, currentValue);
        }

        [TestMethod]
        public void SetClass_ShouldReturnTrue_WhenCurrentValueIsNotNullAndNewValueIsNull()
        {
            var currentValue = new TestClass(1);
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsTrue(result);
            Assert.IsNull(currentValue);
        }

        [TestMethod]
        public void SetClass_ShouldReturnFalse_WhenValuesAreSameInstance()
        {
            var value = new TestClass(1);
            var currentValue = value;
            var newValue = value;

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsFalse(result);
            Assert.AreSame(value, currentValue);
        }

        [TestMethod]
        public void SetClass_ShouldReturnFalse_WhenValuesAreEqualButDifferentInstances()
        {
            var currentValue = new TestClass(1);
            var newValue = new TestClass(1);

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsFalse(result);
            Assert.AreNotSame(newValue, currentValue);
        }

        [TestMethod]
        public void SetClass_ShouldReturnTrue_WhenValuesAreDifferent()
        {
            var currentValue = new TestClass(1);
            var newValue = new TestClass(2);

            var result = SetPropertyUtility.SetClass(ref currentValue, newValue);

            Assert.IsTrue(result);
            Assert.AreSame(newValue, currentValue);
        }
        #endregion

        #region SetStruct
        [TestMethod]
        public void SetStruct_ShouldReturnFalse_WhenValuesAreEqual()
        {
            var currentValue = new TestStruct(1);
            var newValue = new TestStruct(1);

            var result = SetPropertyUtility.SetStruct(ref currentValue, newValue);

            Assert.IsFalse(result);
            Assert.AreEqual(newValue.Value, currentValue.Value);
        }

        [TestMethod]
        public void SetStruct_ShouldReturnTrue_WhenValuesAreDifferent()
        {
            var currentValue = new TestStruct(1);
            var newValue = new TestStruct(2);

            var result = SetPropertyUtility.SetStruct(ref currentValue, newValue);

            Assert.IsTrue(result);
            Assert.AreEqual(newValue.Value, currentValue.Value);
        }
        #endregion
    }
}