using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Model.Maps
{
    public record MapSize : IMapSize
    {
        /// <summary>
        /// Contains array of viewport sizes that are supported by client.
        /// </summary>
        private static readonly int[] _supportedViewportSizes =
        [
            104, 120, 136, 168, 72
        ];

        public static IMapSize Default { get; } = new MapSize(0);
        public static IMapSize Large { get; } = new MapSize(1);
        public static IMapSize VeryLarge { get; } = new MapSize(2);
        public static IMapSize Huge { get; } = new MapSize(3);
        public static IMapSize Small { get; } = new MapSize(4);

        public int Type { get;  }
        public int Size { get; }

        public MapSize(int type) : this(type, _supportedViewportSizes[type]) 
        {
        }

        public MapSize(int type, int size)
        {
            Type = type;
            Size = size;
        }
    }
}
