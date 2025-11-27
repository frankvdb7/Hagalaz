using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using System;

namespace Hagalaz.Game.Scripts.Widgets.Orbs
{
    [StateMetaData("run-energy-orb-resting-state")]
    public class RunEnergyOrbRestingState : RestingState
    {
        public Action? OnRemovedCallback { get; set; }

        public override void OnStateRemoved(IState state, ICreature creature)
        {
            base.OnStateRemoved(state, creature);
            OnRemovedCallback?.Invoke();
        }
    }
}
