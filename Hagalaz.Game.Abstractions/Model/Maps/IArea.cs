using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    public interface IArea
    {
        /// <summary>
        /// Contains area Id.
        /// </summary>
        /// <value>The Id.</value>
        int Id { get; }
        /// <summary>
        /// Whether this area is multi combat zone.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [multi combat]; otherwise, <c>false</c>.
        /// </value>
        bool MultiCombat { get; }
        /// <summary>
        /// Contains area name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
        /// <summary>
        /// Whether this area is pvp.
        /// </summary>
        /// <value><c>true</c> if [PvP]; otherwise, <c>false</c>.</value>
        bool IsPvP { get; }
        /// <summary>
        /// Wether a familiar is allowed in this area.
        /// </summary>
        bool FamiliarAllowed { get; }
        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        IAreaScript Script { get; }
        /// <summary>
        /// Happens when character enters this area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        void OnCreatureEnterArea(ICreature creature);
        /// <summary>
        /// Happens when character exits this area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        void OnCreatureExitArea(ICreature creature);
    }
}
