using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using Hagalaz.Utilities;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class SetPropertyUtilityBenchmark
    {
        private int _intVal1 = 1;
        private int _intVal2 = 2;

        private SimpleStruct _simpleVal1 = new SimpleStruct { Value = 1 };
        private SimpleStruct _simpleVal2 = new SimpleStruct { Value = 2 };

        private EquatableStruct _equatableVal1 = new EquatableStruct { Value = 1 };
        private EquatableStruct _equatableVal2 = new EquatableStruct { Value = 2 };

        private TestClass _classVal1 = new TestClass { Value = "a" };
        private TestClass _classVal2 = new TestClass { Value = "b" };

        public struct SimpleStruct
        {
            public int Value { get; set; }
        }

        public struct EquatableStruct : IEquatable<EquatableStruct>
        {
            public int Value { get; set; }

            public bool Equals(EquatableStruct other) => Value == other.Value;

            public override bool Equals(object? obj) => obj is EquatableStruct other && Equals(other);

            public override int GetHashCode() => Value;
        }

        public class TestClass
        {
            public string Value { get; set; } = default!;

            public override bool Equals(object? obj) => obj is TestClass other && Value == other.Value;

            public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        }

        [Benchmark]
        public bool SetStruct_Primitive_Changed()
        {
            int current = _intVal1;
            return SetPropertyUtility.SetStruct(ref current, _intVal2);
        }

        [Benchmark]
        public bool SetStruct_Primitive_Same()
        {
            int current = _intVal1;
            return SetPropertyUtility.SetStruct(ref current, _intVal1);
        }

        [Benchmark]
        public bool SetStruct_SimpleStruct_Changed()
        {
            SimpleStruct current = _simpleVal1;
            return SetPropertyUtility.SetStruct(ref current, _simpleVal2);
        }

        [Benchmark]
        public bool SetStruct_SimpleStruct_Same()
        {
            SimpleStruct current = _simpleVal1;
            return SetPropertyUtility.SetStruct(ref current, _simpleVal1);
        }

        [Benchmark]
        public bool SetStruct_EquatableStruct_Changed()
        {
            EquatableStruct current = _equatableVal1;
            return SetPropertyUtility.SetStruct(ref current, _equatableVal2);
        }

        [Benchmark]
        public bool SetStruct_EquatableStruct_Same()
        {
            EquatableStruct current = _equatableVal1;
            return SetPropertyUtility.SetStruct(ref current, _equatableVal1);
        }

        [Benchmark]
        public bool SetClass_Changed()
        {
            TestClass? current = _classVal1;
            return SetPropertyUtility.SetClass(ref current, _classVal2);
        }

        [Benchmark]
        public bool SetClass_Same()
        {
            TestClass? current = _classVal1;
            return SetPropertyUtility.SetClass(ref current, _classVal1);
        }
    }
}
