using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFarmingPatch : ITaskItem
    {
        /// <summary>
        /// Contains the current cycle.
        /// </summary>
        int CurrentCycle { get; }
        /// <summary>
        /// Contains the seed definition.
        /// </summary>
        SeedDto? Seed { get; }
        /// <summary>
        /// Contains the patch definition.
        /// </summary>
        PatchDto PatchDefinition { get; }
        /// <summary>
        /// Harvests a single product from this patch.
        /// </summary>
        void Harvest();
        /// <summary>
        /// Resets the times performed.
        /// </summary>
        void Reset();
        /// <summary>
        /// Clears this farming patch (and conditions).
        /// </summary>
        void Clear();
        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Increments the cycle.
        /// </summary>
        void IncrementCycle();
        /// <summary>
        /// Decrements the cycle.
        /// </summary>
        void DecrementCycle();
        /// <summary>
        /// Plants the specified definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        void Plant(SeedDto definition);
        /// <summary>
        /// Determines whether the specified flag has flag.
        /// </summary>
        /// <param name="condition">The flag.</param>
        /// <returns></returns>
        bool HasCondition(PatchCondition condition);
        /// <summary>
        /// Adds the condition.
        /// </summary>
        /// <param name="condition">The condition.</param>
        void AddCondition(PatchCondition condition);
        /// <summary>
        /// Removes the condition.
        /// </summary>
        /// <param name="condition">The condition.</param>
        void RemoveCondition(PatchCondition condition);
    }
}
