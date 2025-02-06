namespace Hagalaz.Cache.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public enum DegradeType
    {
        /// <summary>
        /// Destroy item
        /// </summary>
        DestroyItem = -1,
        /// <summary>
        /// Drop item
        /// </summary>
        DropItem = 0,
        /// <summary>
        /// Protected item (if not at wilderness)
        /// </summary>
        ProtectedItem = 1
    }
}
