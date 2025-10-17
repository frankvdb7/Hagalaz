namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// A data transfer object containing information required to spawn a ground item.
    /// </summary>
    public class GroundItemSpawnDto
    {
        /// <summary>
        /// Gets or sets the ID of the item to spawn.
        /// </summary>
        public int ItemID { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the item.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Gets or sets the number of game ticks until the item respawns after being taken.
        /// </summary>
        public int RespawnTicks { get; set; }

        /// <summary>
        /// Gets or sets the spawn location of the item.
        /// </summary>
        public ILocation Location { get; set; } = default!;
    }
}