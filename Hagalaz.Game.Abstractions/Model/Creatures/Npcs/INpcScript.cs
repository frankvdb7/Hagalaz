using System;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// 
    /// </summary>
    public interface INpcScript : ICreatureScript
    {
        /// <summary>
        /// Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        [Obsolete("Please use NpcScriptMetaData attribute instead")]
        int[] GetSuitableNpcs();
        /// <summary>
        /// Contains the path finder the NPC will use.
        /// By default, this method returns the simple path finder when in combat
        /// and the standart path finder for random walking.
        /// </summary>
        IPathFinder GetPathfinder();
        /// <summary>
        /// Respawns this npc.
        /// By default, this unregisters the NPC if the CanSpawn method returns false.
        /// Otherwise, this calls the Respawn method of the NPC.
        /// </summary>
        void Respawn();
        /// <summary>
        /// Determines whether this instance can respawn.
        /// If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        bool CanRespawn();
        /// <summary>
        /// Render's npc death
        /// </summary>
        /// <returns>Amount of ticks the death gonna be rendered.</returns>
        int RenderDeath();

        /// <summary>
        /// Render's defence of this npc.
        /// </summary>
        /// <param name="delay">Delay in client ticks till attack will reach the target.</param>
        void RenderDefence(int delay);
        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        void Initialize(INpc owner);
        /// <summary>
        /// Happens when attacker attack reaches the npc.
        /// By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which performed attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage inflicted on this character or -1 if it's a miss.</param>
        /// <returns>Return's amount of damage remains after defence.</returns>
        int OnAttack(ICreature attacker, DamageType damageType, int damage);

        /// <summary>
        /// Happens when attacker starts attack to this npc.
        /// By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which started the attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage that is predicted to be inflicted or -1 if it's a miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach this npc and OnAttack will be called.</param>
        /// <returns>Return's amount of damage that remains after defence.</returns>
        int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);
        /// <summary>
        /// Get's if this npc can retaliate to specific character attack.
        /// By default, this method returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns><c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.</returns>
        bool CanRetaliateTo(ICreature creature);
        /// <summary>
        /// Happens when this npc is clicked by specified character ('clicker').
        /// By default this method does for possible attack option.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        /// <param name="forceRun">Wheter clicker should forcerun to this npc.</param>
        void OnCharacterClick(ICharacter clicker, NpcClickType clickType, bool forceRun);
        /// <summary>
        /// Perform's attack on specific target.
        /// By default this method will perform a standart melee attack.
        /// </summary>
        /// <param name="target">The target.</param>
        void PerformAttack(ICreature target);
        /// <summary>
        /// Happens when the attack has been performed on the target.
        /// By default this method will do nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnAttackPerformed(ICreature target);
        /// <summary>
        /// Called when [set target].
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnSetTarget(ICreature target);
        /// <summary>
        /// Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        bool CanSetTarget(ICreature target);
        /// <summary>
        /// Called when [cancel target].
        /// By default, this method will let the NPC walk to its spawnpoint.
        /// </summary>
        void OnCancelTarget();
        /// <summary>
        /// Get's attack bonus type of this npc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>AttackBonus.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Get's attack style of this npc.
        /// By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Get's attack distance of this npc.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetAttackDistance();
        /// <summary>
        /// Get's attack speed of this npc.
        /// By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetAttackSpeed();
        /// <summary>
        /// Get's if this npc can be poisoned.
        /// By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns><c>true</c> if this instance can poison; otherwise, <c>false</c>.</returns>
        bool CanPoison();
        /// <summary>
        /// Determines whether this instance can spawn.
        /// If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        bool CanSpawn();
        /// <summary>
        /// Get's if this npc can be destroyed.
        /// By default , this method returns true.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        bool CanDestroy();
        /// <summary>
        /// Get's if this npc can be suspended.
        /// By default , this method returns true.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        bool CanSuspend();
        /// <summary>
        /// Uses the item on NPC.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool UseItemOnNpc(IItem used, ICharacter character);
        /// <summary>
        /// Get's called when npc is interrupted.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.</param>
        void OnInterrupt(object source);
        /// <summary>
        /// Get's called when npc is destroyed permanently.
        /// By default, this method does nothing.
        /// </summary>
        void OnDestroy();
        /// <summary>
        /// Get's called when npc is created.
        /// By default, this method does nothing.
        /// </summary>
        void OnCreate();
    }
}
