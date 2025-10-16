using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a single farming patch, managing its state, growth cycles, and conditions.
    /// </summary>
    public interface IFarmingPatch : ITaskItem
    {
        /// <summary>
        /// Gets the current growth cycle of the plant in the patch.
        /// </summary>
        int CurrentCycle { get; }
        /// <summary>
        /// Gets the data definition for the seed that is currently planted.
        /// </summary>
        SeedDto? Seed { get; }
        /// <summary>
        /// Gets the data definition for the farming patch itself.
        /// </summary>
        PatchDto PatchDefinition { get; }
        /// <summary>
        /// Performs a harvest action on the patch, gathering one of its products.
        /// </summary>
        void Harvest();
        /// <summary>
        /// Resets the patch to a clear state, ready for a new seed.
        /// </summary>
        void Reset();
        /// <summary>
        /// Clears the patch of any growing plants and removes all conditions.
        /// </summary>
        void Clear();
        /// <summary>
        /// Forces a client-side update for the patch's appearance.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Advances the patch to its next growth cycle.
        /// </summary>
        void IncrementCycle();
        /// <summary>
        /// Reverts the patch to its previous growth cycle.
        /// </summary>
        void DecrementCycle();
        /// <summary>
        /// Plants a new seed in the patch.
        /// </summary>
        /// <param name="definition">The data definition of the seed to plant.</param>
        void Plant(SeedDto definition);
        /// <summary>
        /// Checks if the patch currently has a specific condition.
        /// </summary>
        /// <param name="condition">The condition to check for (e.g., watered, diseased).</param>
        /// <returns><c>true</c> if the patch has the condition; otherwise, <c>false</c>.</returns>
        bool HasCondition(PatchCondition condition);
        /// <summary>
        /// Adds a condition to the patch.
        /// </summary>
        /// <param name="condition">The condition to add.</param>
        void AddCondition(PatchCondition condition);
        /// <summary>
        /// Removes a condition from the patch.
        /// </summary>
        /// <param name="condition">The condition to remove.</param>
        void RemoveCondition(PatchCondition condition);
    }
}
