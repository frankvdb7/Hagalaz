using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Cache.Tests
{
    public static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (expected is IEnumerable e1 && actual is IEnumerable e2 && !(expected is string))
            {
                actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            }
            else
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(expected, actual);
            }
        }
        public static void NotEqual<T>(T expected, T actual)
        {
             if (expected is IEnumerable e1 && actual is IEnumerable e2 && !(expected is string))
            {
                actual.Should().NotBeEquivalentTo(expected);
            }
            else
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotEqual(expected, actual);
            }
        }
        public static void True(bool condition, string? message = null) => condition.Should().BeTrue(message);
        public static void False(bool condition, string? message = null) => condition.Should().BeFalse(message);
        public static void Null(object? obj) => obj.Should().BeNull();
        public static void NotNull(object? obj) => obj.Should().NotBeNull();
        public static void Same(object? expected, object? actual) => actual.Should().BeSameAs(expected);
        public static void NotSame(object? expected, object? actual) => actual.Should().NotBeSameAs(expected);
        public static void IsType<T>(object? obj) => obj.Should().BeOfType<T>();
        public static void IsAssignableFrom<T>(object? obj) => obj.Should().BeAssignableTo<T>();

        public static T Single<T>(IEnumerable<T> collection)
        {
            var list = collection.ToList();
            list.Should().ContainSingle();
            return list[0];
        }

        public static void Empty(IEnumerable? collection)
        {
            if (collection == null) return;
            collection.Cast<object>().Should().BeEmpty();
        }

        public static void NotEmpty(IEnumerable? collection)
        {
            collection.Should().NotBeNull();
            collection!.Cast<object>().Should().NotBeEmpty();
        }

        public static void Collection<T>(IEnumerable<T> collection, params Action<T>[] elementInspectors)
        {
            var list = collection.ToList();
            list.Count.Should().Be(elementInspectors.Length);
            for (int i = 0; i < list.Count; i++)
            {
                elementInspectors[i](list[i]);
            }
        }

        public static T Throws<T>(Action action) where T : Exception
            => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsException<T>(action);

        public static Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
            => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsExceptionAsync<T>(action);
    }
}
