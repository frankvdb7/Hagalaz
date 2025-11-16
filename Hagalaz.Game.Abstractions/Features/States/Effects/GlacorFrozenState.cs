using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Scripts.States; // For GlacorFrozenStateScript

namespace Hagalaz.Game.Abstractions.Features.States.Effects
{
    /// <summary>
    /// Represents the "Glacor Frozen" state.
    /// </summary>
    public class GlacorFrozenState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlacorFrozenState"/> class.
        /// </summary>
        public GlacorFrozenState()
        {
        }

        public override IStateScript Script => new GlacorFrozenStateScript();
    }
}
