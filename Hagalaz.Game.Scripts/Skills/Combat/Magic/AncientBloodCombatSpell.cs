﻿using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all blood blitz,burst,barrage,rush
    /// </summary>
    public class AncientBloodCombatSpell : ICombatSpell
    {
        /// <summary>
        ///     Construct's new ancient blood combat spell.
        /// </summary>
        public AncientBloodCombatSpell(int spellType) => _spellType = spellType;

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] RuneRequirements =
        [
            [
                RuneType.Chaos, RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Chaos, RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Death, RuneType.Blood, RuneType.Soul
            ]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] RuneAmounts =
        [
            [
                2, 2, 1
            ],
            [
                4, 2, 2
            ],
            [
                2, 4
            ],
            [
                4, 2, 1
            ]
        ];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] LevelRequirements =
        [
            56, 68, 80, 92
        ];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] ConfigIds =
        [
            67, 75, 83, 91
        ];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] Exp =
        [
            33.0, 39.0, 45.0, 51.0
        ];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] BaseDamage =
        [
            150, 210, 250, 290
        ];

        private readonly int _spellType;

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = _spellType == 1 || _spellType == 3;
            caster.QueueAnimation(Animation.Create(multiHit ? 1979 : 1978));
            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, Exp[_spellType]);

            var vDeltaX = caster.Location.X - victim.Location.X;
            var vDeltaY = caster.Location.Y - victim.Location.Y;
            if (vDeltaX < 0)
            {
                vDeltaX = -vDeltaX;
            }

            if (vDeltaY < 0)
            {
                vDeltaY = -vDeltaY;
            }


            if (_spellType == 2)
            {
                var projectile = new Projectile(374);
                projectile.SetSenderData(caster, 43, false);
                projectile.SetReceiverData(victim, 0);
                projectile.SetFlyingProperties(51, (short)(vDeltaX * 5 + vDeltaY * 5), 16, 64, false);
                projectile.Display();
            }


            var visibleCreatures = caster.Viewport.VisibleCreatures;
            foreach (var c in visibleCreatures)
            {
                if (c != victim)
                {
                    if (!multiHit || !caster.Area.MultiCombat || !victim.Area.MultiCombat || !c.Combat.CanBeAttackedBy(caster))
                    {
                        continue;
                    }

                    if (victim is ICharacter && !victim.Area.IsPvP || c is ICharacter && !c.Area.IsPvP)
                    {
                        continue;
                    }
                }

                if (c == caster || c.Location.X < victim.Location.X - 1 || c.Location.X > victim.Location.X + 1
                    || c.Location.Y < victim.Location.Y - 1 || c.Location.Y > victim.Location.Y + 1)
                {
                    continue;
                }

                var creature = c;
                var max = combat.GetMagicMaxHit(creature, BaseDamage[_spellType]);
                var damage = combat.GetMagicDamage(creature, max);

                var deltaX = caster.Location.X - creature.Location.X;
                var deltaY = caster.Location.Y - creature.Location.Y;
                if (deltaX < 0)
                {
                    deltaX = -deltaX;
                }

                if (deltaY < 0)
                {
                    deltaY = -deltaY;
                }

                combat.PerformSoulSplit(creature, damage);

                damage = creature.Combat.IncomingAttack(caster, DamageType.FullMagic, damage, (byte)(51 + deltaX * 5 + deltaY * 5));
                combat.AddMagicExperience(damage);

                var delay = (byte)(51 + vDeltaX * 5 + vDeltaY * 5);

                caster.Statistics.HealLifePoints((int)(damage * 0.25));

                if (damage == -1)
                {
                    creature.QueueGraphic(Graphic.Create(85, delay, 150));
                }
                else switch (_spellType)
                {
                    case 0:
                        creature.QueueGraphic(Graphic.Create(373, delay));
                        break;
                    case 1:
                        creature.QueueGraphic(Graphic.Create(376, delay));
                        break;
                    case 2:
                        creature.QueueGraphic(Graphic.Create(375, delay));
                        break;
                    case 3:
                        creature.QueueGraphic(Graphic.Create(377, delay));
                        break;
                }

                caster.QueueTask(new RsTask(() =>
                    {
                        var soak = -1;
                        var dmg = creature.Combat.Attack(caster, DamageType.FullMagic, damage, ref soak);
                        if (dmg == -1)
                        {
                            return;
                        }

                        var splat = new HitSplat(caster);
                        splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= BaseDamage[_spellType]);
                        if (soak != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                        }

                        creature.QueueHitSplat(splat);
                    }, CreatureHelper.CalculateTicksForClientTicks(delay)));
            }
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <returns></returns>
        public bool CanAttack(ICharacter caster, ICreature victim) => true;

        /// <summary>
        ///     Check's requirements for this spell.
        /// </summary>
        /// <returns></returns>
        public bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(LevelRequirements[_spellType]) && caster.Magic.CheckRunes(RuneRequirements[_spellType], RuneAmounts[_spellType]);

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(RuneRequirements[_spellType], RuneAmounts[_spellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        /// <param name="activatedOn"></param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, ConfigIds[_spellType]);

        /// <summary>
        ///     Get's called when autocasting is unset to this spell.
        /// </summary>
        /// <param name="deactivatedOn"></param>
        public void OnAutoCastingDeactivation(ICharacter deactivatedOn) => deactivatedOn.Configurations.SendStandardConfiguration(108, -1);

        /// <summary>
        ///     Get's speed of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public int GetCastingSpeed(ICharacter caster) => 5;

        /// <summary>
        ///     Get's combat distance of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public int GetCombatDistance(ICharacter caster) => 8;

        /// <summary>
        ///     Get's amount of magic experience this spell gives.
        /// </summary>
        /// <returns></returns>
        public double GetMagicExperience() => Exp[_spellType];
    }
}