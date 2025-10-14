using Xunit;

namespace Hagalaz.Utilities.Tests
{
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

        [Fact]
        public void SetClass_WhenValueChanged_ReturnsTrue()
        {
            var oldValue = new TestClass { Value = "a" };
            var newValue = new TestClass { Value = "b" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.True(result);
            Assert.Equal(newValue, oldValue);
        }

        [Fact]
        public void SetClass_WhenValueChangesFromNull_ReturnsTrue()
        {
            TestClass? oldValue = null;
            var newValue = new TestClass { Value = "a" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.True(result);
            Assert.Equal(newValue, oldValue);
        }

        [Fact]
        public void SetClass_WhenValueChangesToNull_ReturnsTrue()
        {
            var oldValue = new TestClass { Value = "a" };
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.True(result);
            Assert.Null(oldValue);
        }

        [Fact]
        public void SetClass_WhenValuesAreSame_ReturnsFalse()
        {
            var oldValue = new TestClass { Value = "a" };
            var newValue = new TestClass { Value = "a" };

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.False(result);
            Assert.Equal(newValue, oldValue);
        }

        [Fact]
        public void SetClass_WhenBothValuesAreNull_ReturnsFalse()
        {
            TestClass? oldValue = null;
            TestClass? newValue = null;

            var result = SetPropertyUtility.SetClass(ref oldValue, newValue);

            Assert.False(result);
            Assert.Null(oldValue);
        }

        [Fact]
        public void SetStruct_WhenValueChanged_ReturnsTrue()
        {
            var oldValue = new TestStruct { Value = 1 };
            var newValue = new TestStruct { Value = 2 };

            var result = SetPropertyUtility.SetStruct(ref oldValue, newValue);

            Assert.True(result);
            Assert.Equal(newValue, oldValue);
        }

        [Fact]
        public void SetStruct_WhenValuesAreSame_ReturnsFalse()
        {
            var oldValue = new TestStruct { Value = 1 };
            var newValue = new TestStruct { Value = 1 };

            var result = SetPropertyUtility.SetStruct(ref oldValue, newValue);

            Assert.False(result);
            Assert.Equal(newValue, oldValue);
        }
    }
}
