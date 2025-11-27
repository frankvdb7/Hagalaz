using Hagalaz.Game.Abstractions.Model.Creatures;
using System;

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Staff of Light Special Effect" state.
    /// </summary>
    [StateMetaData("staff-of-light-special-effect-state")]
    public class StaffOfLightSpecialEffectState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StaffOfLightSpecialEffectState"/> class.
        /// </summary>
        public StaffOfLightSpecialEffectState()
        {
        }

        /// <summary>
        /// Gets or sets the callback to be invoked when the state is removed.
        /// </summary>
        public Action? OnRemovedCallback { get; set; }

        /// <inheritdoc />
        public override void OnStateRemoved(IState state, ICreature creature)
        {
            base.OnStateRemoved(state, creature);
            OnRemovedCallback?.Invoke();
        }
    }
}
