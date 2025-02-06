using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model
{
    public interface IProjectile
    {
        /// <summary>
        /// Contains location from which projectile is comming,
        /// can't be null.
        /// </summary>
        ILocation FromLocation { get; }

        /// <summary>
        /// Contains location to which projectile is comming,
        /// can't be null.
        /// </summary>
        ILocation ToLocation { get; }

        /// <summary>
        /// Contains sender character, can be null.
        /// </summary>
        ICreature? FromCreature { get; }

        /// <summary>
        /// Contains receiver character , can be null.
        /// </summary>
        ICreature? ToCreature { get; }

        /// <summary>
        /// Boolean which defines if starting height should be adjusted.
        /// </summary>
        bool AdjustFromFlyingHeight { get; }

        /// <summary>
        /// Boolean which defines if projectile height should be adjusted when it's
        /// flying so it doesn't fly thru mountains for example.
        /// </summary>
        bool AdjustToFlyingHeight { get; }

        /// <summary>
        /// Id of the body part from which sender is sending projectile.
        /// Only usable if AdjustSenderStartHeight is enabled and Sender is not null.
        /// </summary>
        int FromBodyPartId { get; }

        /// <summary>
        /// Contains projectile graphic Id.
        /// </summary>
        int GraphicId { get; }

        /// <summary>
        /// Contains projectile delay. 1 = 25ms , 2 = 50ms n so on. 
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// Contains projectile duration, higher duration - lower speed , 
        /// lower duration - higher speed.
        /// 1 = 25ms , 2 = 50ms n so on.
        /// </summary>
        int Duration { get; }

        /// <summary>
        /// Contains starting height of the projectile.
        /// </summary>
        int FromHeight { get; }

        /// <summary>
        /// Contains end height of the projectile.
        /// </summary>
        int ToHeight { get; }

        /// <summary>
        /// Contains projectile slope.
        /// </summary>
        int Slope { get; }

        /// <summary>
        /// Contains projectile angle.
        /// </summary>
        int Angle { get; }
    }
}