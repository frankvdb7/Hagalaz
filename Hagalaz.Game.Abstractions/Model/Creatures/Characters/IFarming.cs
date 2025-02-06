namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFarming
    {
        /// <summary>
        /// Removes the farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        void RemoveFarmingPatch(int patchObjectID);
        /// <summary>
        /// Gets the farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        /// <returns></returns>
        IFarmingPatch? GetFarmingPatch(int patchObjectID);
        /// <summary>
        /// Gets the or create farming patch.
        /// </summary>
        /// <param name="patchObjectID">The patch object identifier.</param>
        /// <returns></returns>
        IFarmingPatch GetOrCreateFarmingPatch(int patchObjectID);
    }
}
