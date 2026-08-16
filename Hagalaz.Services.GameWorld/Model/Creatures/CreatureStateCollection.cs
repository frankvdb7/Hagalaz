using System;
using System.Buffers;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Stores and processes the states owned by one creature.
    /// </summary>
    internal sealed class CreatureStateCollection
    {
        private readonly ICreature _creature;
        private readonly Dictionary<Type, IState> _states = new();

        public CreatureStateCollection(ICreature creature) => _creature = creature;

        public IEnumerable<IState> States => _states.Values;

        public bool Has(Type stateType) => _states.ContainsKey(stateType);

        public void Add(IState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            var stateType = state.GetType();
            if (_states.TryGetValue(stateType, out var existingState))
            {
                if (state is not IKeepLongestDurationState newTimedState ||
                    existingState is not IKeepLongestDurationState existingTimedState ||
                    newTimedState.TicksLeft <= existingTimedState.TicksLeft)
                {
                    return;
                }

                Remove(stateType, existingState);
                if (_states.ContainsKey(stateType))
                {
                    // A removal callback installed another instance. Keep that
                    // callback-owned transition authoritative and avoid replacing it.
                    return;
                }
            }

            _states.Add(stateType, state);
        }

        public void Remove(Type stateType) => Remove(stateType, expectedState: null);

        public void ProcessTick()
        {
            if (_states.Count == 0)
            {
                return;
            }

            var statesCount = _states.Count;
            var statesBuffer = ArrayPool<IState>.Shared.Rent(statesCount);
            try
            {
                _states.Values.CopyTo(statesBuffer, 0);
                for (var i = 0; i < statesCount; i++)
                {
                    var state = statesBuffer[i];
                    if (!IsActive(state))
                    {
                        continue;
                    }

                    if (state is not ITimedState timedState)
                    {
                        continue;
                    }

                    if (timedState.TicksLeft > 0)
                    {
                        timedState.TicksLeft--;
                    }

                    if (timedState.TicksLeft <= 0)
                    {
                        Remove(state.GetType(), state);
                    }
                }
            }
            finally
            {
                ArrayPool<IState>.Shared.Return(statesBuffer, clearArray: true);
            }
        }

        private bool IsActive(IState state) => _states.TryGetValue(state.GetType(), out var activeState) && ReferenceEquals(activeState, state);

        private void Remove(Type stateType, IState? expectedState)
        {
            if (!_states.TryGetValue(stateType, out var state) || expectedState is not null && !ReferenceEquals(state, expectedState))
            {
                return;
            }

            _states.Remove(stateType);
            NotifyRemoved(state);
        }

        private void NotifyRemoved(IState state)
        {
            if (state is IStateLifecycle lifecycle)
            {
                lifecycle.OnRemoved(_creature);
            }
        }
    }
}
