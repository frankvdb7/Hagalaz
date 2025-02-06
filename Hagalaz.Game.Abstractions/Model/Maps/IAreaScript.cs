using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAreaScript
    {
        /// <summary>
        /// Initialize's script with specific area.
        /// </summary>
        /// <param name="area">The area.</param>
        void Initialize(IArea area);
        /// <summary>
        /// Happens when character enters this area.
        /// The new area, is this scripts area.
        /// By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        void OnCreatureEnterArea(ICreature creature);
        /// <summary>
        /// Happens when character exits this area.
        /// The old area, is this scripts area.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        void OnCreatureExitArea(ICreature creature);
        /// <summary>
        /// This method get's called when given character enters
        /// this area.
        /// The new area, is this scripts area.
        /// By default this method does enable/disable multicombat icon
        /// and adds/removes Attack options on characters if the ('character') is character.
        /// </summary>
        /// <param name="creature">The creature.</param>
        void RenderEnterArea(ICreature creature);
        /// <summary>
        /// This method get's called when given character exits
        /// this area.
        /// The old area, is this scripts area.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        void RenderExitArea(ICreature creature);
        /// <summary>
        /// Get's if specified character ('victim') which is in this area,
        /// can be attacked by the specified character ('attacker') which can be in any area.
        /// By default , before returning true this method checks if victim is being attacked by someone other
        /// or this zone is multicombat.
        /// </summary>
        /// <param name="victim">Creature which is being attacked by the ('attacker')</param>
        /// <param name="attacker">Creature which is attacking the ('victim')</param>
        /// <returns>
        /// If attack can be performed.
        /// </returns>
        bool CanBeAttacked(ICreature victim, ICreature attacker);
        /// <summary>
        /// Get's if specific character ('attacker') which is in this area,
        /// can attack the specified character ('target') which can be in any area.
        /// By default , before returning true this method checks if target is being attacked by someone other
        /// or this zone is multicombat.
        /// </summary>
        /// <param name="attacker">Creature which is attacking the ('target')</param>
        /// <param name="target">Creature which is being attacked by ('attacker')</param>
        /// <returns>
        /// If attack can be performed.
        /// </returns>
        bool CanAttack(ICreature attacker, ICreature target);
        /// <summary>
        /// Get's if character can do standart teleport. (Escape)
        /// By default , this method returns true.
        /// </summary>
        /// <param name="character">Character which is trying to 'escape'</param>
        /// <returns><c>true</c> if this instance [can do standart teleport] the specified character; otherwise, <c>false</c>.</returns>
        bool CanDoStandardTeleport(ICharacter character);
        /// <summary>
        /// Determines whether this instance [can do game object teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can do game object teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        bool CanDoGameObjectTeleport(ICharacter character, IGameObject obj);
        /// <summary>
        /// Get's respawn location of specific character in this area.
        /// By default this method does return World.SpawnPoint.
        /// </summary>
        /// <param name="character">Character which needs respawning.</param>
        /// <returns>Location.</returns>
        ILocation GetRespawnLocation(ICharacter character);
        /// <summary>
        /// Gets the pvp combat level range.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        int GetPvPCombatLevelRange(ICharacter character);
        /// <summary>
        /// Get's if this area should render base combat level (without summoning) and then render extra + (summoning level)
        /// By default , this method does return false.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool ShouldRenderBaseCombatLevel(ICharacter character);
    }
}
