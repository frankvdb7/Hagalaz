using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model
{
    /// <summary>
    /// Projectile class.
    /// </summary>
    public class Projectile : IProjectile
    {
        /// <summary>
        /// Contains location from which projectile is comming,
        /// can't be null.
        /// </summary>
        public ILocation FromLocation { get; init; } = default!;

        /// <summary>
        /// Contains size X of the tile from which projectile is coming.
        /// </summary>
        public int FromTileSizeX { get; private set; }

        /// <summary>
        /// Contains size Y of the tile from which projectile is coming.
        /// </summary>
        public int FromTileSizeY { get; private set; }

        /// <summary>
        /// Contains location to which projectile is comming,
        /// can't be null.
        /// </summary>
        public ILocation ToLocation { get; init; } = default!;

        /// <summary>
        /// Contains size X of the tile to which projectile is going.
        /// </summary>
        public int ToTileSizeX { get; private set; }

        /// <summary>
        /// Contains size Y of the tile to which projectile is going.
        /// </summary>
        public int ToTileSizeY { get; private set; }

        /// <summary>
        /// Contains sender character, can be null.
        /// </summary>
        public ICreature? FromCreature { get; init; }

        /// <summary>
        /// Contains receiver character , can be null.
        /// </summary>
        public ICreature? ToCreature { get; init; }

        /// <summary>
        /// Contains projectile graphic Id.
        /// </summary>
        public int GraphicId { get; }

        /// <summary>
        /// Contains projectile delay. 1 = 25ms , 2 = 50ms n so on.
        /// </summary>
        public int Delay { get; init; }

        /// <summary>
        /// Contains projectile duration, higher duration - lower speed , 
        /// lower duration - higher speed.
        /// 1 = 25ms , 2 = 50ms n so on.
        /// </summary>
        public int Duration { get; init; }

        /// <summary>
        /// Contains starting height of the projectile.
        /// </summary>
        public int FromHeight { get; init; }

        /// <summary>
        /// Contains end height of the projectile.
        /// </summary>
        public int ToHeight { get; init; }

        /// <summary>
        /// Contains projectile slope.
        /// </summary>
        public int Slope { get; init; }

        /// <summary>
        /// Contains projectile angle.
        /// </summary>
        public int Angle { get; init; }

        /// <summary>
        /// Boolean which defines if starting height should be adjusted.
        /// </summary>
        public bool AdjustFromFlyingHeight { get; init; }

        /// <summary>
        /// Boolean which defines if projectile height should be adjusted when it's
        /// flying so it doesn't fly thru mountains for example.
        /// </summary>
        public bool AdjustToFlyingHeight { get; init; }

        /// <summary>
        /// Id of the body part from which sender is sending projectile.
        /// Only usable if AdjustSenderStartHeight is enabled and Sender is not null.
        /// </summary>
        public int FromBodyPartId { get; private set; }

        /// <summary>
        /// Construct's new projectile.
        /// </summary>
        /// <param name="graphicID">Id of the graphic projectile should be displaying.</param>
        [Obsolete("Use the IProjectileBuilder instead")]
        public Projectile(int graphicID) => GraphicId = graphicID;
    }
}