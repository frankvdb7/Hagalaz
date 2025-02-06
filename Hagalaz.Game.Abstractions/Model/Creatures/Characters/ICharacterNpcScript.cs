using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterNpcScript
    {
        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner"></param>
        void Initialize(ICharacter owner);
        /// <summary>
        /// Happens when character is turned to npc for 
        /// which this script is made.
        /// This method usually get's called after Initialize()
        /// By default, this method does nothing.
        /// </summary>
        void OnTurnToNpc();
        /// <summary>
        /// Happens when character is turned back to player 
        /// and the script is about to be disposed, this method gives
        /// opportunity to release used resources.
        /// By default, this method does nothing.
        /// </summary>
        void OnTurnToPlayer();
        /// <summary>
        /// Get's called when pnpc is dead.
        /// By default, this method does nothing.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// Get's called when pnpc is spawned.
        /// By default, this method does nothing.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Get's attack distance of this pnpc.
        /// By default, this method returns 1.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetAttackDistance();
        /// <summary>
        /// Get's attack speed of this pnpc.
        /// By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetAttackSpeed();
        /// <summary>
        /// Gets the maximum hitpoints.
        /// </summary>
        /// <returns></returns>
        int GetMaximumHitpoints();
        /// <summary>
        /// Get's attack bonus type of this pnpc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>AttackBonus.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Get's attack style of this pnpc.
        /// By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <returns></returns>
        string GetDisplayName();
        /// <summary>
        /// Gets the combat level.
        /// </summary>
        /// <returns></returns>
        int GetCombatLevel();
        /// <summary>
        /// Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        void PerformAttack(ICreature target);
        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// Determines whether this instance [can be looted by] the specified killer.
        /// By default, this method returns true.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// Get's called when pnpc is killed.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="killer">The killer.</param>
        void OnDeathBy(ICreature killer);
        /// <summary>
        /// Render's this pnpc death.
        /// </summary>
        /// <returns>Amount of ticks to wait before respawning.</returns>
        int RenderDeath();

        /// <summary>
        /// Render's defence of this pnpc.
        /// </summary>
        /// <param name="delay">Delay in client ticks till attack will reach the target.</param>
        void RenderDefence(int delay);
        /// <summary>
        /// Ticks this instance.
        /// </summary>
        void Tick();
    }
}
