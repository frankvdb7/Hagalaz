using System;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Model
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
        public ILocation FromLocation { get; set; } = default!;

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
        public ILocation ToLocation { get; set; } = default!;

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
        public ICreature? FromCreature { get; set; }

        /// <summary>
        /// Contains receiver character , can be null.
        /// </summary>
        public ICreature? ToCreature { get; set; }

        /// <summary>
        /// Contains projectile graphic Id.
        /// </summary>
        public int GraphicId { get; }

        /// <summary>
        /// Contains projectile delay. 1 = 25ms , 2 = 50ms n so on. 
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// Contains projectile duration, higher duration - lower speed , 
        /// lower duration - higher speed.
        /// 1 = 25ms , 2 = 50ms n so on.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Contains starting height of the projectile.
        /// </summary>
        public int FromHeight { get; set; }

        /// <summary>
        /// Contains end height of the projectile.
        /// </summary>
        public int ToHeight { get; set; }

        /// <summary>
        /// Contains projectile slope.
        /// </summary>
        public int Slope { get; set; }

        /// <summary>
        /// Contains projectile angle.
        /// </summary>
        public int Angle { get; set; }

        /// <summary>
        /// Boolean which defines if starting height should be adjusted.
        /// </summary>
        public bool AdjustFromFlyingHeight { get; set; }

        /// <summary>
        /// Boolean which defines if projectile height should be adjusted when it's
        /// flying so it doesn't fly thru mountains for example.
        /// </summary>
        public bool AdjustToFlyingHeight { get; set; }

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

        /// <summary>
        /// Set's sender data of this projectile.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="startHeight">The start height.</param>
        /// <param name="adjustSenderStartHeight">if set to <c>true</c> [adjust sender start height].</param>
        /// <param name="senderBodyPartID">The sender body part identifier.</param>
        /// <returns></returns>
        public Projectile SetSenderData(ICreature sender, int startHeight, bool adjustSenderStartHeight = true, int senderBodyPartID = 0)
        {
            FromCreature = sender;
            FromLocation = sender.Location.Clone();
            FromTileSizeX = FromTileSizeY = (short)sender.Size;
            FromHeight = startHeight;
            AdjustFromFlyingHeight = adjustSenderStartHeight;
            FromBodyPartId = senderBodyPartID;
            return this;
        }

        /// <summary>
        /// Set's sender data of this projectile.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="startHeight">The start height.</param>
        /// <returns></returns>
        public Projectile SetSenderData(IGameObject sender, int startHeight)
        {
            FromLocation = sender.Location.Clone();
            FromTileSizeX = sender.Definition.SizeX;
            FromTileSizeY = sender.Definition.SizeY;
            FromHeight = startHeight;
            return this;
        }

        /// <summary>
        /// Set's sender data of this projectile.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="startHeight">The start height.</param>
        /// <returns></returns>
        public Projectile SetSenderData(IGroundItem item, int startHeight = 0)
        {
            FromLocation = item.Location.Clone();
            FromTileSizeX = FromTileSizeY = 1;
            FromHeight = startHeight;
            return this;
        }

        /// <summary>
        /// Set's sender data of this projectile.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="startHeight">The start height.</param>
        /// <param name="tileSizeX">The tile size x.</param>
        /// <param name="tileSizeY">The tile size y.</param>
        /// <returns></returns>
        public Projectile SetSenderData(ILocation from, int startHeight, int tileSizeX = 1, int tileSizeY = 1)
        {
            FromLocation = from;
            FromTileSizeX = tileSizeX;
            FromTileSizeY = tileSizeY;
            FromHeight = startHeight;
            return this;
        }

        /// <summary>
        /// Set's receive data of this projectile.
        /// </summary>
        /// <param name="receiver">The receiver.</param>
        /// <param name="endHeight">The end height.</param>
        /// <returns></returns>
        public Projectile SetReceiverData(ICreature receiver, int endHeight)
        {
            ToCreature = receiver;
            ToLocation = receiver.Location.Clone();
            ToTileSizeX = ToTileSizeY = receiver.Size;
            ToHeight = endHeight;
            return this;
        }

        /// <summary>
        /// Set's receiver data of this projectile.
        /// </summary>
        /// <param name="receiver">The receiver.</param>
        /// <param name="endHeight">The end height.</param>
        /// <returns></returns>
        public Projectile SetReceiverData(IGameObject receiver, int endHeight)
        {
            ToLocation = receiver.Location.Clone();
            ToTileSizeX = receiver.Definition.SizeX;
            ToTileSizeY = receiver.Definition.SizeY;
            ToHeight = endHeight;
            return this;
        }

        /// <summary>
        /// Set's receiver data of this projectile.
        /// </summary>
        /// <param name="receiver">The receiver.</param>
        /// <param name="endHeight">The end height.</param>
        /// <returns></returns>
        public Projectile SetReceiverData(IGroundItem receiver, int endHeight = 0)
        {
            ToLocation = receiver.Location.Clone();
            ToTileSizeX = ToTileSizeY = 1;
            ToHeight = endHeight;
            return this;
        }

        /// <summary>
        /// Set's receive data of this projectile.
        /// </summary>
        /// <param name="to">To.</param>
        /// <param name="endHeight">The end height.</param>
        /// <param name="tileSizeX">The tile size x.</param>
        /// <param name="tileSizeY">The tile size y.</param>
        /// <returns></returns>
        public Projectile SetReceiverData(ILocation to, int endHeight, int tileSizeX = 1, int tileSizeY = 1)
        {
            ToLocation = to;
            ToTileSizeX = tileSizeX;
            ToTileSizeY = tileSizeY;
            ToHeight = endHeight;
            return this;
        }

        /// <summary>
        /// Set's flying properties of this projectile.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="slope">The slope.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="adjustFlyingHeight">if set to <c>true</c> [adjust flying height].</param>
        /// <returns></returns>
        public Projectile SetFlyingProperties(int delay, int duration, int slope, int angle, bool adjustFlyingHeight = true)
        {
            Delay = delay;
            Duration = duration;
            Slope = slope;
            Angle = angle;
            AdjustToFlyingHeight = adjustFlyingHeight;
            return this;
        }

        /// <summary>
        /// Display's this projectile.
        /// </summary>
        public void Display()
        {
            // TODO - remove this method and replace
            var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
            var region = regionManager.GetOrCreateMapRegion(FromLocation.RegionId, FromLocation.Dimension, true);
            //region.QueueUpdate(new DrawProjectileUpdate(this));
        }
    }
}