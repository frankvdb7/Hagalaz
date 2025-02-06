using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Contains region related methods for npc.
    /// </summary>
    public partial class Npc : Creature
    {
        /// <summary>
        /// Happens when npc region changes.
        /// </summary>
        protected override void OnRegionChange() {}

        /// <summary>
        /// Informs npc that it must add itself to given region.
        /// </summary>
        /// <param name="newRegion"></param>
        protected override void AddToRegion(IMapRegion newRegion) => newRegion.Add(this);

        /// <summary>
        /// Informs npc that it must remove itself from given region.
        /// </summary>
        /// <param name="region"></param>
        protected override void RemoveFromRegion(IMapRegion region) => region.Remove(this);
    }
}