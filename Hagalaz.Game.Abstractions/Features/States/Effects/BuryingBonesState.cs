using System;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    [StateMetaData("burying-bones-state")]
    public class BuryingBonesState : TimedState, IStateLifecycle, IKeepLongestDurationState
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
