using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Model.Creatures.Characters
{
    /// <summary>
    /// Base Player npc script class.
    /// </summary>
    public abstract class CharacterNpcScriptBase : ICharacterNpcScript
    {
        /// <summary>
        /// Contains owner character.
        /// </summary>
        protected ICharacter Owner { get; private set; }

        /// <summary>
        /// Contains the NPC definition.
        /// </summary>
        protected INpcDefinition Definition { get; private set; }

        /// <summary>
        /// The NPC definitionRepository
        /// </summary>
        private INpcService _npcDefinitionRepository;

        /// <summary>
        /// Get's called when owner is found.
        /// </summary>
        protected virtual void Initialize() {}

        /// <summary>
        /// Happens when character is turned to npc for 
        /// which this script is made.
        /// This method usually get's called after Initialize()
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnTurnToNpc() { }

        /// <summary>
        /// Happens when character is turned back to player 
        /// and the script is about to be disposed, this method gives
        /// opportunity to release used resources.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnTurnToPlayer() { }

        /// <summary>
        /// Get's called when pnpc is spawned.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// Get's called when pnpc is dead.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnDeath() { }

        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void OnTargetKilled(ICreature target) { }

        /// <summary>
        /// Get's called when pnpc is killed.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="killer">The killer.</param>
        public virtual void OnDeathBy(ICreature killer) { }

        /// <summary>
        /// Determines whether this instance [can be looted by] the specified killer.
        /// By default, this method returns true.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        public virtual bool CanBeLootedBy(ICreature killer) => true;

        /// <summary>
        /// Get's attack bonus type of this pnpc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>AttackBonus.</returns>
        public virtual AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        /// Get's attack style of this pnpc.
        /// By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        public virtual AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;

        /// <summary>
        /// Get's attack speed of this pnpc.
        /// By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public virtual int GetAttackSpeed() => Definition.AttackSpeed;

        /// <summary>
        /// Get's attack distance of this pnpc.
        /// By default, this method returns 1.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public virtual int GetAttackDistance() => 1;

        /// <summary>
        /// Gets the maximum hitpoints.
        /// </summary>
        /// <returns></returns>
        public virtual int GetMaximumHitpoints() => Definition.MaxLifePoints;

        /// <summary>
        /// Gets the type of the hp bar.
        /// </summary>
        /// <returns></returns>
        public virtual short GetHpBarType() => 5;

        /// <summary>
        /// Gets the combat level.
        /// </summary>
        /// <returns></returns>
        public virtual int GetCombatLevel() => Definition.CombatLevel;

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <returns></returns>
        public virtual string GetDisplayName() => Definition.Name;

        /// <summary>
        /// Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void PerformAttack(ICreature target)
        {
            RenderAttack();
            var damage = ((ICharacterCombat)Owner.Combat).GetMeleeDamage(target, false);
            var maxDamage = ((ICharacterCombat)Owner.Combat).GetMeleeMaxHit(target, false);
            Owner.Combat.PerformAttack(new AttackParams()
            {
                Target = target, Damage = damage, MaxDamage = maxDamage, DamageType = DamageType.StandardMelee,
            });
        }

        /// <summary>
        /// Render's this pnpc death.
        /// </summary>
        /// <returns>Amount of ticks to wait before respawning.</returns>
        public virtual int RenderDeath()
        {
            if (Definition.DeathAnimation != -1)
                Owner.QueueAnimation(Animation.Create(Definition.DeathAnimation));
            if (Definition.DeathGraphic != -1)
                Owner.QueueGraphic(Graphic.Create(Definition.DeathGraphic));
            return Definition.DeathTicks;
        }

        /// <summary>
        /// Render's standart attack.
        /// This method is not guaranteed to render correct attack.
        /// By default, it is called by default implementation of PerformAttack method.
        /// </summary>
        public virtual void RenderAttack()
        {
            if (Definition.AttackAnimation != -1)
                Owner.QueueAnimation(Animation.Create(Definition.AttackAnimation));
            if (Definition.AttackGraphic != -1)
                Owner.QueueGraphic(Graphic.Create(Definition.AttackGraphic));
        }

        /// <summary>
        /// Render's defence of this pnpc.
        /// </summary>
        /// <param name="delay">Delay in client ticks till attack will reach the target.</param>
        public virtual void RenderDefence(int delay)
        {
            if (Definition.DefenceAnimation != -1)
                Owner.QueueAnimation(Animation.Create(Definition.DefenceAnimation, delay));
            if (Definition.DefenseGraphic != -1)
                Owner.QueueGraphic(Graphic.Create(Definition.DefenseGraphic, delay));
        }

        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner"></param>
        public void Initialize(ICharacter owner)
        {
            Owner = owner;
            _npcDefinitionRepository = owner.ServiceProvider.GetRequiredService<INpcService>();
            Definition = _npcDefinitionRepository.FindNpcDefinitionById(Owner.Appearance.NpcId);
            Initialize();
        }

        /// <summary>
        /// Tick's pnpc.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void Tick() { }
    }
}