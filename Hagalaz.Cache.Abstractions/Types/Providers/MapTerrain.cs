namespace Hagalaz.Cache.Abstractions.Types.Providers
{
    /// <summary>
    /// Terrain flag rules shared by map decoding and dynamic map reconstruction.
    /// </summary>
    public static class MapTerrain
    {
        public const int MapPlanes = 4;
        public const int MapDimension = 64;
        public const int BlockedFlag = 0x1;
        public const int BridgeFlag = 0x2;

        public static bool IsBlocked(sbyte flags) => (flags & BlockedFlag) != 0;

        public static bool IsBridge(sbyte flags) => (flags & BridgeFlag) != 0;

        /// <summary>
        /// The plane collision and objects use for a tile drawn on <paramref name="plane"/>:
        /// one lower when plane 1 of that tile is a bridge, as the client does.
        /// </summary>
        public static int GetCollisionPlane(int plane, bool isBridge) => isBridge ? plane - 1 : plane;
    }
}
