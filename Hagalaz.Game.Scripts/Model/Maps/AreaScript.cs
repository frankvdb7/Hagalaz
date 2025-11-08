using System.Linq;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Resources;
using Microsoft.Extensions.Options;

namespace Hagalaz.Game.Scripts.Model.Maps
{
    /// <summary>
    /// Class AreaScript
    /// </summary>
    public abstract class AreaScript : IAreaScript
    {
        /// <summary>
        /// Contains area instance.
        /// </summary>
        /// <value>The area.</value>
        protected IArea Area { get; private set; }

        private ILocation SpawnPoint { get; set; }

        /// <summary>
        /// Initializes area script.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Happens when character enters this area.
        /// The new area, is this scripts area.
        /// By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        public virtual void OnCreatureEnterArea(ICreature creature)
        {
            if (Area.FamiliarAllowed)
            {
                return;
            }

            if (creature is not ICharacter character)
            {
                return;
            }

            if (character.HasFamiliar())
                character.EventManager.SendEvent(new FamiliarDismissEvent(character));
        }

        /// <summary>
        /// Happens when character exits this area.
        /// The old area, is this scripts area.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public virtual void OnCreatureExitArea(ICreature creature) { }

        /// <summary>
        /// This method get's called when given character enters
        /// this area.
        /// The new area, is this scripts area.
        /// By default this method does enable/disable multicombat icon
        /// and adds/removes Attack options on characters if the ('character') is character.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public virtual void RenderEnterArea(ICreature creature)
        {
            if (creature is not ICharacter c)
            {
                return;
            }

            c.Configurations.SendGlobalCs2Int(616, Area.MultiCombat ? 1 : 0);
            if (Area.IsPvP)
            {
                c.RegisterCharactersOptionHandler(CharacterClickType.Option2Click, "Attack", 65535, true, (target, forceRun) =>
                {
                    c.Interrupt(this);
                    c.Movement.MovementType = c.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                    c.FaceLocation(target);
                    c.Combat.SetTarget(target);
                });
            }
            else
                c.UnregisterCharactersOptionHandler(CharacterClickType.Option2Click);
        }

        /// <summary>
        /// This method get's called when given character exits
        /// this area.
        /// The old area, is this scripts area.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public virtual void RenderExitArea(ICreature creature) { }

        /// <summary>
        /// Gets the PvP combat level range.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual int GetPvPCombatLevelRange(ICharacter character) => byte.MaxValue;

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
        public virtual bool CanAttack(ICreature attacker, ICreature target)
        {
            if (!Area.IsPvP && attacker is ICharacter character1 && target is ICharacter)
            {
                character1.SendChatMessage(GameStrings.Combat_SourceNeedPvPZone);
                return false;
            }

            if (!Area.IsPvP && attacker is INpc npc && npc.HasScript<IFamiliarScript>() && target is ICharacter)
                return false;
            if (!Area.IsPvP && attacker is INpc npc1 && npc1.HasScript<IFamiliarScript>() && target is INpc npc2 && npc2.HasScript<IFamiliarScript>())
                return false;
            if (Area.MultiCombat)
                return true;
            if (attacker is INpc npc3 && npc3.HasScript<IFamiliarScript>() && target is INpc npc4 && npc4.HasScript<IFamiliarScript>())
                return false;
            if (attacker is ICharacter && target is INpc npc5 && npc5.HasScript<IFamiliarScript>())
                return false;
            var attackers = attacker.Combat.RecentAttackers;
            if (attackers.All(a => a.Attacker == target))
            {
                return true;
            }

            if (attacker is ICharacter character)
                character.SendChatMessage(GameStrings.Combat_SomeoneAttacking);
            return false;
        }

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
        public virtual bool CanBeAttacked(ICreature victim, ICreature attacker)
        {
            if (!Area.IsPvP && victim is ICharacter && attacker is ICharacter character1)
            {
                character1.SendChatMessage(GameStrings.Combat_VictimNeedPvPZone);
                return false;
            }

            if (!Area.IsPvP && victim is INpc npc && npc.HasScript<IFamiliarScript>() && attacker is ICharacter character2)
            {
                character2.SendChatMessage(GameStrings.Combat_VictimNeedPvPZone);
                return false;
            }

            if (!Area.IsPvP && attacker is INpc npc1 && npc1.HasScript<IFamiliarScript>() && victim is INpc npc2 && npc2.HasScript<IFamiliarScript>())
                return false;
            if (Area.MultiCombat)
                return true;
            if (attacker is ICharacter && victim is INpc npc3 && npc3.HasScript<IFamiliarScript>())
                return false;
            if (attacker is INpc npc4 && npc4.HasScript<IFamiliarScript>() && victim is ICharacter)
                return false;
            var attackers = victim.Combat.RecentAttackers;
            if (attackers.All(a => a.Attacker == attacker))
            {
                return true;
            }

            if (attacker is ICharacter character)
                character.SendChatMessage(GameStrings.Combat_TargetAttacked);
            return false;
        }

        /// <summary>
        /// Get's if this area should render base combat level (without summoning) and then render extra + (summoning level)
        /// By default , this method does return false.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public virtual bool ShouldRenderBaseCombatLevel(ICharacter character) => false;

        /// <summary>
        /// Get's respawn location of specific character in this area.
        /// By default this method does return RandomStatic.SpawnPoint.
        /// </summary>
        /// <param name="character">Character which needs respawning.</param>
        /// <returns>Location.</returns>
        public virtual ILocation GetRespawnLocation(ICharacter character) => SpawnPoint;

        /// <summary>
        /// Get's if character can do standart teleport. (Escape)
        /// By default , this method returns true.
        /// </summary>
        /// <param name="character">Character which is trying to 'escape'</param>
        /// <returns><c>true</c> if this instance [can do standart teleport] the specified character; otherwise, <c>false</c>.</returns>
        public virtual bool CanDoStandardTeleport(ICharacter character) => true;

        /// <summary>
        /// Determines whether this instance [can do item teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can do item teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanDoItemTeleport(ICharacter character, IItem item) => true;

        /// <summary>
        /// Determines whether this instance [can do game object teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can do game object teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanDoGameObjectTeleport(ICharacter character, IGameObject obj) => true;

        /// <summary>
        /// Initialize's script with specific area.
        /// </summary>
        /// <param name="area">The area.</param>
        public void Initialize(IArea area)
        {
            Area = area;
            var options = ServiceLocator.Current.GetInstance<IOptions<WorldOptions>>();
            SpawnPoint = Location.Create(options.Value.SpawnPointX, options.Value.SpawnPointY, options.Value.SpawnPointZ);
            Initialize();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public sealed override bool Equals(object obj) => obj is AreaScript && GetType().IsInstanceOfType(obj);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => base.GetHashCode();
    }
}