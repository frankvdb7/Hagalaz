using System;
using System.Collections.Generic;
using Hagalaz.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public class CombatPhase<T> where T : ICombatRotation
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
        /// The active rotation
        /// </summary>
        private T _activeRotation;

        /// <summary>
        /// The rotations
        /// </summary>
        private readonly List<T> _rotations = null;

        /// <summary>
        /// The active
        /// </summary>
        private bool _active = false;

        /// <summary>
        /// Gets a value indicating whether this <see cref="CombatPhase{T}"/> is active.
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
        /// Gets or sets the active rotation.
        /// </summary>
        /// <value>
        /// The active rotation.
        /// </value>
        public T ActiveRotation
        {
            get => _activeRotation;
            set
            {
                if (_activeRotation == null && value != null || !_activeRotation.Equals(value))
                {
                    _activeRotation?.Deactivate();
                    _activeRotation = value;
                    _activeRotation?.Activate();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatPhase{T}"/> class.
        /// </summary>
        public CombatPhase() => _rotations = new List<T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatPhase{T}"/> class.
        /// </summary>
        /// <param name="rotations">The rotations.</param>
        public CombatPhase(List<T> rotations) => _rotations = rotations;

        /// <summary>
        /// Activates this instance.
        /// </summary>
        public void Activate() => Active = true;

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        public void Deactivate() => Active = false;

        /// <summary>
        /// Adds the rotation.
        /// </summary>
        /// <param name="rotation">The rotation.</param>
        /// <returns>This instance.</returns>
        public void AddRotation(T rotation) => _rotations.Add(rotation);

        /// <summary>
        /// Removes the rotation.
        /// </summary>
        /// <param name="rotation">The rotation.</param>
        public void RemoveRotation(T rotation) => _rotations.Remove(rotation);

        /// <summary>
        /// Selects the new rotation.
        /// </summary>
        /// <param name="selector">The selector.</param>
        public void SelectNewRotation(Func<IList<T>, T> selector) => ActiveRotation = selector(_rotations);
    }
}