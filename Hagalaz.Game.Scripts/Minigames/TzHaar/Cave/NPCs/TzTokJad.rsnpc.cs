using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2745])]
    public class TzTokJad : StandardCaveNpc
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        /// </summary>
        private enum Attack
        {
            Melee,
            Ranged,
            Magic
        }

        /// <summary>
        ///     The attack style.
        /// </summary>
        private Attack _attack = Attack.Ranged;

        /// <summary>
        ///     The healers spawned.
        /// </summary>
        private bool _healersSpawned;

        public TzTokJad(INpcBuilder npcBuilder, IProjectileBuilder projectileBuilder) : base(npcBuilder)
        {
            _projectileBuilder = projectileBuilder;
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
                case Attack.Melee: return AttackBonus.Crush;
                case Attack.Ranged: return AttackBonus.Ranged;
                case Attack.Magic: return AttackBonus.Magic;
                default: return base.GetAttackBonusType();
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
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 8;

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
                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target),
                            DamageType = DamageType.StandardMelee,
                            Target = target,
                            MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target)
                        });
                        break;
                    }
                case Attack.Ranged:
                    {
                        Owner.QueueAnimation(Animation.Create(16202));
                        Owner.QueueGraphic(Graphic.Create(2994));

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var delay = 25 + deltaX * 5 + deltaY * 5;
                        //Projectile projectile = new Projectile(12);
                        //projectile.SetSenderData(this.owner, 50, false);
                        // projectile.SetReceiverData(target, 35);
                        // projectile.SetFlyingProperties(10, delay, 10, 0, false);
                        //  projectile.Display();

                        var handle = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                            MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                            DamageType = DamageType.StandardRange,
                            Delay = delay,
                            Target = target
                        });

                        handle.RegisterResultHandler(_ => { target.QueueGraphic(Graphic.Create(3000)); });
                        break;
                    }
                case Attack.Magic:
                    {
                        Owner.QueueAnimation(Animation.Create(16195));
                        Owner.QueueGraphic(Graphic.Create(2995));

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var delay = 50 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(2996)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(delay)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .Send();

                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 500),
                            DamageType = DamageType.StandardMagic,
                            Target = target,
                            Delay = delay,
                            MaxDamage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 500)
                        });
                        break;
                    }
            }

            GenerateAttackType(target);
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature target)
        {
            var chances = new double[5];
            chances[0] = 0.70; // melee
            if (target != null && !Owner.WithinRange(target, 1))
            {
                chances[0] = 0.0;
            }

            chances[1] = 0.15; // ranged
            chances[2] = 0.15; // magic
            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (hitValue < runningValue)
                {
                    if (i == 0)
                    {
                        _attack = Attack.Melee;
                    }
                    else if (i == 1)
                    {
                        _attack = Attack.Ranged;
                    }
                    else if (i == 2)
                    {
                        _attack = Attack.Magic;
                    }

                    return;
                }
            }
        }

        /// <summary>
        ///     Tick's npc.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (_healersSpawned || Owner.Definition.MaxLifePoints / 2 < Owner.Statistics.LifePoints)
            {
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                NpcBuilder.Create().WithId(2746).WithLocation(Owner.Location).Spawn();
            }

            _healersSpawned = true;
        }
    }
}