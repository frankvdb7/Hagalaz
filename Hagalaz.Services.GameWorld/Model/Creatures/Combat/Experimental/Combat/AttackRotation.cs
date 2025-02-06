using System;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public class AttackRotation : ICombatRotation
    {
        /// <summary>
        /// Occurs when [on active].
        /// </summary>
        public event Action OnActivated;

        /// <summary>
        /// Occurs when [on deactivated].
        /// </summary>
        public event Action OnDeactivated;

        /// <summary>
        /// Occurs when [on perform].
        /// </summary>
        public event Action OnPerform;

        /// <summary>
        /// Avalue indicating whether this <see cref="AttackRotation"/> is active.
        /// </summary>
        private bool _active = false;

        /// <summary>
        /// The probability function
        /// </summary>
        private readonly Func<double> _probabilityFunc;

        /// <summary>
        /// Gets or sets the chance.
        /// </summary>
        /// <value>
        /// The chance.
        /// </value>
        public double Probability => _probabilityFunc();

        /// <summary>
        /// Gets a value indicating whether this <see cref="AttackRotation"/> is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool Active
        {
            get => _active;
            private set
            {
                if (SetPropertyUtility.SetStruct(ref _active, value))
                {
                    if (value) OnActivated?.Invoke();
                    else OnDeactivated?.Invoke();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackRotation"/> class.
        /// </summary>
        /// <param name="probability">The probability.</param>
        public AttackRotation(Func<double> probability) => _probabilityFunc = probability;

        /// <summary>
        /// Activates this instance.
        /// </summary>
        public void Activate() => Active = true;

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        public void Deactivate() => Active = false;

        /// <summary>
        /// Performs this instance.
        /// </summary>
        public void Perform() => OnPerform?.Invoke();
    }
}