using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class ProcessStatesBenchmark
    {
        private Dictionary<Type, IStateMock> _states = null!;
        private Dictionary<Type, IStateMock> _statesTemplate = null!;

        [Params(0, 1, 5, 20)]
        public int StateCount;

        private static readonly Type[] TypesPool = new[]
        {
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(short), typeof(ushort), typeof(byte), typeof(sbyte),
            typeof(double), typeof(float), typeof(decimal), typeof(bool),
            typeof(char), typeof(string), typeof(object), typeof(DateTime),
            typeof(Guid), typeof(TimeSpan), typeof(Int128), typeof(UInt128)
        };

        [GlobalSetup]
        public void Setup()
        {
            _statesTemplate = new Dictionary<Type, IStateMock>();
            for (int i = 0; i < StateCount; i++)
            {
                var type = TypesPool[i % TypesPool.Length];
                _statesTemplate[type] = new StateMock { TicksLeft = 10 };
            }

            if (StateCount > 0)
            {
                // Make one expired
                _statesTemplate.Values.First().TicksLeft = 0;
            }

            _states = new Dictionary<Type, IStateMock>(_statesTemplate);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset the dictionary for each iteration to ensure consistent state
            _states.Clear();
            foreach(var kvp in _statesTemplate)
            {
                _states.Add(kvp.Key, kvp.Value);
            }
        }

        [Benchmark(Baseline = true)]
        public void Old_ProcessStates()
        {
            foreach (var state in _states.Values.ToList())
            {
                state.Tick();
                if (state.TicksLeft <= 0)
                {
                    _states.Remove(state.GetType());
                    // Simulate RemoveState logic
                    state.OnStateRemoved(state, null!);
                }
            }
        }

        [Benchmark]
        public void New_ProcessStates_Manual_ArrayPool()
        {
            if (_states.Count == 0) return;

            var statesCount = _states.Count;
            var statesBuffer = ArrayPool<IStateMock>.Shared.Rent(statesCount);

            try
            {
                _states.Values.CopyTo(statesBuffer, 0);

                Type[]? toRemove = null;
                var removeCount = 0;

                for (var i = 0; i < statesCount; i++)
                {
                    var state = statesBuffer[i];
                    state.Tick();
                    if (state.TicksLeft <= 0)
                    {
                        toRemove ??= ArrayPool<Type>.Shared.Rent(statesCount);
                        toRemove[removeCount++] = state.GetType();
                    }
                }

                if (toRemove != null)
                {
                    try
                    {
                        for (var i = 0; i < removeCount; i++)
                        {
                            if (_states.Remove(toRemove[i], out var state))
                            {
                                state.OnStateRemoved(state, null!);
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<Type>.Shared.Return(toRemove, true);
                    }
                }
            }
            finally
            {
                ArrayPool<IStateMock>.Shared.Return(statesBuffer, true);
            }
        }

        [Benchmark]
        public void New_ProcessStates_FastPath_ArrayPool()
        {
            var statesCount = _states.Count;
            if (statesCount == 0) return;

            if (statesCount == 1)
            {
                Type? stateType = null;
                IStateMock? state = null;
                foreach (var kvp in _states)
                {
                    stateType = kvp.Key;
                    state = kvp.Value;
                    break;
                }

                if (state != null && stateType != null)
                {
                    state.Tick();
                    if (state.TicksLeft <= 0)
                    {
                        if (_states.Remove(stateType, out var removedState))
                        {
                            removedState.OnStateRemoved(removedState, null!);
                        }
                    }
                }
                return;
            }

            var statesBuffer = ArrayPool<IStateMock>.Shared.Rent(statesCount);

            try
            {
                _states.Values.CopyTo(statesBuffer, 0);

                Type[]? toRemove = null;
                var removeCount = 0;

                for (var i = 0; i < statesCount; i++)
                {
                    var state = statesBuffer[i];
                    state.Tick();
                    if (state.TicksLeft <= 0)
                    {
                        toRemove ??= ArrayPool<Type>.Shared.Rent(statesCount);
                        toRemove[removeCount++] = state.GetType();
                    }
                }

                if (toRemove != null)
                {
                    try
                    {
                        for (var i = 0; i < removeCount; i++)
                        {
                            if (_states.Remove(toRemove[i], out var state))
                            {
                                state.OnStateRemoved(state, null!);
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<Type>.Shared.Return(toRemove, true);
                    }
                }
            }
            finally
            {
                ArrayPool<IStateMock>.Shared.Return(statesBuffer, true);
            }
        }

        // Mock interfaces/classes
        public interface IStateMock
        {
            int TicksLeft { get; set; }
            void Tick();
            void OnStateRemoved(IStateMock state, object creature);
        }

        public class StateMock : IStateMock
        {
            public int TicksLeft { get; set; }
            public void Tick() { /* simulation */ }
            public void OnStateRemoved(IStateMock state, object creature) { /* simulation */ }
        }
    }
}
