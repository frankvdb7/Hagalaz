namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Class which contains data about ground item spawn.
    /// </summary>
    public class GroundItemSpawnDto
    { 
        /// <summary>
        /// Id of the item.
        /// </summary>
        public int ItemID { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// The respawn ticks.
        /// </summary>
        public int RespawnTicks { get; set; }

        /// <summary>
        /// Location of the object.
        /// </summary>
        public ILocation Location { get; set; } = default!;
    }
}