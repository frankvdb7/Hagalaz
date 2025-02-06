namespace Hagalaz.Game.Utilities
{
    /// <summary>
    /// Primitive game object helper class
    /// </summary>
    public static class GameObjectHelper
    {
        /// <summary>
        /// Gets the local region hash from the supplied coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static int GetRegionLocalHash(int x, int y, int z, int layer) => LocationHelper.GetRegionLocalHash(x, y, z) | layer << 15;
    }
}
