namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's Farming skill data, including their various farming patches.
    /// </summary>
    public interface IFarming
    {
        /// <summary>
        /// Removes a farming patch associated with a specific game object ID.
        /// </summary>
        /// <param name="patchObjectID">The unique ID of the game object representing the patch.</param>
        void RemoveFarmingPatch(int patchObjectID);
        /// <summary>
        /// Retrieves an existing farming patch by its associated game object ID.
        /// </summary>
        /// <param name="patchObjectID">The unique ID of the game object representing the patch.</param>
        /// <returns>The <see cref="IFarmingPatch"/> if it exists; otherwise, <c>null</c>.</returns>
        IFarmingPatch? GetFarmingPatch(int patchObjectID);
        /// <summary>
        /// Retrieves an existing farming patch or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="patchObjectID">The unique ID of the game object representing the patch.</param>
        /// <returns>The existing or newly created <see cref="IFarmingPatch"/>.</returns>
        IFarmingPatch GetOrCreateFarmingPatch(int patchObjectID);
    }
}
