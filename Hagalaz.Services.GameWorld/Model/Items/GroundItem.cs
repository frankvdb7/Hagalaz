using System;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Model.Items
{
    public class GroundItem : IGroundItem
    {
        public IItem ItemOnGround { get; }
        public ILocation Location { get; }
        public IMapRegion Region
        {
            get
            {
                var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
                return regionManager.GetOrCreateMapRegion(Location.RegionId, Location.Dimension, false);
            }
        }
        public ICharacter? Owner { get; set; }
        public int TicksLeft { get; set; }
        public int RespawnTicks { get; private set; }
        public bool IsRespawning { get; private set; }
        public bool IsPublic => Owner == null;
        public int Size => 0;
        public string Name => ItemOnGround.Name;
        public bool IsDestroyed { get; private set; }

        private GroundItem(IItem itemOnGround, ILocation location)
        {
            ItemOnGround = itemOnGround;
            Location = location;
        }

        public GroundItem(
            IItem itemOnGround,
            ILocation location,
            ICharacter? owner,
            int respawnTicks,
            int ticksLeft,
            bool isRespawning = false)
        {
            ItemOnGround = itemOnGround;
            Location = location;
            Owner = owner;
            RespawnTicks = respawnTicks;
            TicksLeft = ticksLeft;
            IsRespawning = isRespawning;
        }

        public bool CanRespawn() => RespawnTicks > 0 && !IsRespawning;


        /// <summary>
        /// Happens when this ground item is spawned.
        /// </summary>
        public void OnSpawn() { }

        /// <summary>
        /// Destroys this entity.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"{this} is already destroyed!");
            }
            IsDestroyed = true;
        }

        /// <summary>
        /// Get's if this entity can be destroyed automatically.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        public bool CanDestroy()
        {
            if (CanRespawn() && IsRespawning || TicksLeft > 0)
                return false;
            return true;
        }

        /// <summary>
        /// Get's if this entity can be suspended.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        public bool CanSuspend()
        {
            if (CanRespawn() && IsRespawning || TicksLeft > 0)
                return false;
            return true;
        }

        /// <summary>
        /// Despawns this instance.
        /// </summary>
        /// <returns></returns>
        public bool Despawn()
        {
            Region.Remove(this);
            return true;
        }

        /// <summary>
        /// Determines whether this instance can stack the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if this instance can stack the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanStack(IGroundItem item)
        {
            if (CanRespawn() || IsRespawning)
                return false;
            if (ItemOnGround.ItemScript.CanStackItem(ItemOnGround, item.ItemOnGround, false))
                return true;
            return false;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public IGroundItem Clone() => new GroundItem(
            ItemOnGround.Clone(),
            Location.Clone(),
            Owner,
            RespawnTicks,
            TicksLeft,
            IsRespawning);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"item[{ItemOnGround.Name},loc=({Location})]";
    }
}