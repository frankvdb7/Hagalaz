using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    [StateMetaData("eating-state")]
    public class EatingState : TimedState, IStateLifecycle, IKeepLongestDurationState
    {
        public Action? OnRemovedCallback { get; set; }

        public void OnRemoved(ICreature creature)
        {
            OnRemovedCallback?.Invoke();
        }

        public void OnAdded(ICreature creature)
        {
        }
    }
}
