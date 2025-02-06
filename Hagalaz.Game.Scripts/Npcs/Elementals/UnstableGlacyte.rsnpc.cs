﻿using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Elementals
{
    /// <summary>
    /// </summary>
    public class UnstableGlacyte : NpcScriptBase
    {
        /// <summary>
        ///     The charge ticks.
        /// </summary>
        private const int ChargeTicks = 10;

        /// <summary>
        ///     The charge.
        /// </summary>
        private int _charge;

        /// <summary>
        ///     The healing.
        /// </summary>
        private bool _healing;

        /// <summary>
        ///     The glacor
        /// </summary>
        private readonly INpc _glacor;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnstableGlacyte" /> class.
        /// </summary>
        public UnstableGlacyte() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnstableGlacyte" /> class.
        /// </summary>
        /// <param name="glacor">The glacor.</param>
        public UnstableGlacyte(INpc glacor) => _glacor = glacor;

        /// <summary>
        ///     Called when [set target].
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target)
        {
            if (_glacor != null && _glacor.Combat.Target == null)
            {
                _glacor.QueueTask(new RsTask(() => _glacor.Combat.SetTarget(target), 1));
            }
        }

        /// <summary>
        ///     Starts the charge.
        /// </summary>
        private void StartCharge() => _charge = 0;

        /// <summary>
        ///     Heals this instance.
        /// </summary>
        private void Heal()
        {
            if (!_healing)
            {
                return;
            }

            if (Owner.Statistics.LifePoints < Owner.Definition.MaxLifePoints / 3)
            {
                Owner.Statistics.HealLifePoints(10);
            }
            else
            {
                _healing = false;
                StartCharge();
            }
        }

        /// <summary>
        ///     Explodes this instance.
        /// </summary>
        private void Explode()
        {
            Owner.Movement.Lock(true);
            Owner.Movement.ClearQueue();
            Owner.QueueGraphic(Graphic.Create(439));
            Owner.QueueAnimation(Animation.Create(9964));

            var npcDamage = (int)(Owner.Statistics.LifePoints * 0.9);
            Owner.Statistics.DamageLifePoints(npcDamage);
            var npcSplat = new HitSplat(Owner);
            npcSplat.SetFirstSplat(npcDamage == -1 ? HitSplatType.HitMiss : HitSplatType.HitSimpleDamage, npcDamage == -1 ? 0 : npcDamage, true);
            Owner.QueueHitSplat(npcSplat);

            var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.WithinRange(Owner, 1));
            foreach (var character in characters)
            {
                var dmg = character.Combat.IncomingAttack(Owner, DamageType.Reflected, character.Statistics.LifePoints / 3, 0);
                var soak = -1;
                var damage = character.Combat.Attack(Owner, DamageType.Reflected, dmg, ref soak);
                var splat = new HitSplat(Owner);
                splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitSimpleDamage, damage == -1 ? 0 : damage, dmg <= damage);
                if (soak != -1)
                {
                    splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                }

                character.QueueHitSplat(splat);
            }

            _healing = true;
            Owner.QueueTask(new RsTask(() => { Owner.Movement.Unlock(true); }, 2));
        }

        /// <summary>
        ///     Get's called when npc is spawned.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnSpawn() => StartCharge();

        /// <summary>
        ///     Get's called when npc is dead.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDeath() => _healing = false;

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (Owner.Combat.Target == null || Owner.Combat.Target == attacker)
            {
                return base.CanBeAttackedBy(attacker);
            }

            (attacker as ICharacter)?.SendChatMessage("Someone else is already fighting that glacyte.");

            return false;

        }

        /// <summary>
        ///     Get's if this npc can retaliate to specific character attack.
        ///     By default, this method returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) => Owner.Combat.Target == null;

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [14302];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Tick's npc.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (!Owner.Combat.IsInCombat() || Owner.Combat.IsDead)
            {
                return;
            }

            if (_healing)
            {
                Heal();
            }
            else if (++_charge >= ChargeTicks)
            {
                Explode();
            }
        }
    }
}