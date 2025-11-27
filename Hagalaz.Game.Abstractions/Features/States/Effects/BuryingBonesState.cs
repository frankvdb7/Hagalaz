using System;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    [StateMetaData("burying-bones-state")]
    public class BuryingBonesState : State
    {
        public Action? OnRemovedCallback { get; set; }

        public override void OnStateRemoved(IState state, ICreature creature)
        {
            base.OnStateRemoved(state, creature);
            OnRemovedCallback?.Invoke();
        }
    }
}
