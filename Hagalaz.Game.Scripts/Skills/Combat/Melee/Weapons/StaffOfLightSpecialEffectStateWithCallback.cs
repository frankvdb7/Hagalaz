using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using System;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    [StateMetaData("staff-of-light-special-effect-state-with-callback")]
    public class StaffOfLightSpecialEffectStateWithCallback : StaffOfLightSpecialEffectState
    {
        public Action? OnRemovedCallback { get; set; }

        public override void OnStateRemoved(IState state, ICreature creature)
        {
            base.OnStateRemoved(state, creature);
            OnRemovedCallback?.Invoke();
        }
    }
}
