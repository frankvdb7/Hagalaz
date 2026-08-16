using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Represents one checked item-container mutation and its exact rollback.
    /// </summary>
    public sealed class ItemContainerMutation
    {
        private readonly int _appliedCount;
        private readonly IReadOnlyList<IItem> _appliedItems;
        private readonly Func<bool> _rollback;
        private bool _rolledBack;

        internal ItemContainerMutation(
            int appliedCount,
            bool succeeded,
            IReadOnlyList<IItem> appliedItems,
            Func<bool> rollback)
        {
            _appliedCount = appliedCount;
            Succeeded = succeeded;
            _appliedItems = appliedItems;
            _rollback = rollback;
        }

        /// <summary>
        /// Gets the number of item units applied by the mutation.
        /// </summary>
        public int AppliedCount => _rolledBack ? 0 : _appliedCount;

        /// <summary>
        /// Gets a value indicating whether the complete requested mutation was applied.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the exact item delta applied by an add mutation.
        /// </summary>
        public IReadOnlyList<IItem> AppliedItems => _rolledBack ? [] : _appliedItems;

        /// <summary>
        /// Gets a value indicating whether the mutation changed the container.
        /// </summary>
        public bool HasChanges => AppliedCount > 0;

        /// <summary>
        /// Attempts to restore the exact pre-mutation slot and item state.
        /// </summary>
        public bool TryRollback()
        {
            if (_rolledBack || _appliedCount == 0)
            {
                return true;
            }

            var restored = _rollback();
            if (restored)
            {
                _rolledBack = true;
            }

            return restored;
        }
    }
}
