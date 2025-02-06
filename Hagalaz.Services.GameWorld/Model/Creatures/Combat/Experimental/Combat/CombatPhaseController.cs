using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public class CombatPhaseController<T> where T : ICombatRotation
    {
        /// <summary>
        /// The active phase
        /// </summary>
        private CombatPhase<T> _activePhase;

        /// <summary>
        /// Gets or sets the active phase.
        /// </summary>
        /// <value>
        /// The active phase.
        /// </value>
        public CombatPhase<T> ActivePhase
        {
            get => _activePhase;
            set
            {
                var oldPhase = _activePhase;
                if (SetPropertyUtility.SetClass(ref _activePhase, value))
                {
                    oldPhase?.Deactivate();
                    _activePhase?.Activate();
                }
            }
        }
    }
}