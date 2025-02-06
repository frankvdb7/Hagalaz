namespace Hagalaz.Game.Utilities
{
    /// <summary>
    /// Primitive helper class for locations / coordinates
    /// </summary>
    public static class LocationHelper
    {
        /// <summary>
        /// Converts a coordinate from a local plane to the absolute coordinate of the global plane.
        /// </summary>
        /// <param name="localCoordinate"></param>
        /// <param name="gridCoordinate"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static int ConvertLocalToAbsolute(int localCoordinate, int gridCoordinate, int gridSize) => localCoordinate + gridCoordinate * gridSize;
        /// <summary>
        /// Gets the local region hash from the supplied coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static int GetRegionLocalHash(int x, int y, int z) => (x & 0x3F) | ((y & 0x3F) << 6) | (z << 12);
        /// <summary>
        /// Gets the part hash from the supplied coordinates.
        /// </summary>
        /// <param name="partX"></param>
        /// <param name="partY"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static int GetRegionPartHash(int partX, int partY, int z) => (partX & 0x3ff) | ((partY & 0x7ff) << 10) | ((z & 0x3) << 21);
    }
}
