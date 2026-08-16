using System;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    ///     Contains the RestingState.
    /// </summary>
    [StateMetaData("resting-state")]
    public class RestingState : State, IStateLifecycle
    {
        public Action? OnRemovedCallback { get; set; }

        public void OnRemoved(ICreature creature) => OnRemovedCallback?.Invoke();
    }
}
