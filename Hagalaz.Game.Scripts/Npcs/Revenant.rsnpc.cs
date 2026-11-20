using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Npcs
{
    /// <summary>
    /// </summary>
    internal enum Attack
    {
        /// <summary>
        ///     The melee
        /// </summary>
        Melee,

        /// <summary>
        ///     The ranged
        /// </summary>
        Ranged,

        /// <summary>
        ///     The magic
        /// </summary>
        Magic
    }

    /// <summary>
    /// </summary>
    [NpcScriptMetaData([
        13465, 13466, 13467, 13468, 13469, 13470, 13471, 13472, 13473, 13474, 13475, 13476, 13477, 13478, 13479, 13480, 13481
    ])]
    public class Revenant : NpcScriptBase
    {
        private readonly INpcAttackService _attacks;

        /// <summary>
        ///     The attack.
        /// </summary>
        private Attack _attack;

        public Revenant(INpc owner, INpcAttackService attacks, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
            _attacks = attacks;
            Owner.AddState(new NpcTypeUndeadState());
        }

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick() { }

        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature) => false;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attack)
            {
                case Attack.Melee:
                    {
                        RenderAttack();
                        var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
                        var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Target = target.Handle, Damage = damage, MaxDamage = maxDamage, DamageType = DamageType.StandardMelee,
                        });

                        if (RandomStatic.Generator.NextDouble() >= 0.90)
                        {
                            target.Poison(58);
                        }

                        break;
                    }
                case Attack.Ranged:
                    {
                        RenderAttack();

                        _attacks.PerformRanged(Owner, target, new NpcProjectile(1278, 50));
                        break;
                    }
                case Attack.Magic:
                    {
                        RenderAttack();

                        _attacks.PerformMagic(Owner, target, new NpcProjectile(1278, 50), 164);
                        break;
                    }
            }

            GenerateAttack();
        }

        /// <summary>
        ///     Generates the attack.
        /// </summary>
        private void GenerateAttack()
        {
            var chances = new double[5];
            chances[0] = 0.75; // melee
            chances[1] = 0.10; // ranged
            chances[2] = 0.10; // magic
            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (!(hitValue < runningValue))
                {
                    continue;
                }

                switch (i)
                {
                    case 0: _attack = Attack.Melee; break;
                    case 1: _attack = Attack.Ranged; break;
                    case 2: _attack = Attack.Magic; break;
                }

                return;
            }
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance()
        {
            switch (_attack)
            {
                case Attack.Melee: return 1;
                case Attack.Ranged:
                case Attack.Magic:
                    return 7;
                default: return base.GetAttackDistance();
            }
        }

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_attack)
            {
                case Attack.Melee: return AttackStyle.MeleeAggressive;
                case Attack.Ranged: return AttackStyle.RangedAccurate;
                case Attack.Magic: return AttackStyle.MagicNormal;
                default: return base.GetAttackStyle();
            }
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType()
        {
            switch (_attack)
            {
                case Attack.Melee: return AttackBonus.Slash;
                case Attack.Ranged: return AttackBonus.Ranged;
                case Attack.Magic: return AttackBonus.Magic;
                default: return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Get's called when npc is spawned.
        /// </summary>
        public override void OnSpawn() => _attack = (Attack)RandomStatic.Generator.Next(0, 3);

    }
}
