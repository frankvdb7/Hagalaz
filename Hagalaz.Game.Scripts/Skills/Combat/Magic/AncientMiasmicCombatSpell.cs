﻿using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
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
    ///     Class for all miasmic blitz,burst,barrage,rush
    /// </summary>
    public class AncientMiasmicCombatSpell : ICombatSpell
    {
        /// <summary>
        ///     Contains type of the spell.
        /// </summary>
        public int SpellType { get; }

        /// <summary>
        ///     Construct's new ancient miasmic combat spell.
        /// </summary>
        public AncientMiasmicCombatSpell(int spellType) => SpellType = spellType;

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        public static RuneType[][] RuneRequirements = [
            [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Blood, RuneType.Earth, RuneType.Soul],
            [RuneType.Blood, RuneType.Earth, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        public static int[][] RuneAmounts = [[2, 1, 1], [4, 2, 2], [2, 3, 3], [4, 4, 4]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        public static int[] LevelRequirements = [61, 73, 85, 97];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        public static int[] ConfigIds = [95, 97, 99, 101];

        /// <summary>
        ///     The EXP
        /// </summary>
        public static double[] Exp = [36.0, 42.0, 48.0, 54.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        public static int[] BaseDamage = [180, 240, 280, 352];

        /// <summary>
        ///     The STAF f_ IDS
        /// </summary>
        public static int[] StaffIds = [13867, 13869, 13941, 13943];

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = SpellType == 1 || SpellType == 3;

            switch (SpellType)
            {
                case 0:
                    caster.QueueAnimation(Animation.Create(10513));
                    break;
                case 1:
                    caster.QueueAnimation(Animation.Create(10516));
                    break;
                case 2:
                    caster.QueueAnimation(Animation.Create(10524));
                    break;
                case 3:
                    caster.QueueAnimation(Animation.Create(10518));
                    break;
            }

            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, Exp[SpellType]);

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


            switch (SpellType)
            {
                case 0:
                {
                    caster.QueueGraphic(Graphic.Create(1845));
                    var projectile = new Projectile(1846);
                    projectile.SetSenderData(caster, 43, false);
                    projectile.SetReceiverData(caster, 22);
                    projectile.SetFlyingProperties(51, (short)(vDeltaX * 5 + vDeltaY * 5), 16, 64, false);
                    projectile.Display();
                    break;
                }
                case 1:
                    caster.QueueGraphic(Graphic.Create(1848));
                    break;
                case 2:
                {
                    caster.QueueGraphic(Graphic.Create(1850));
                    var projectile = new Projectile(1852);
                    projectile.SetSenderData(caster, 43, false);
                    projectile.SetReceiverData(caster, 22);
                    projectile.SetFlyingProperties(51, (short)(vDeltaX * 5 + vDeltaY * 5), 16, 64, false);
                    projectile.Display();
                    break;
                }
                case 3:
                    caster.QueueGraphic(Graphic.Create(1853));
                    break;
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

                // render skull if conditions are right
                if (c != victim)
                {
                    combat.CheckSkullConditions(c);
                }

                var creature = c;
                var max = combat.GetMagicMaxHit(creature, BaseDamage[SpellType]);
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

                if (c is ICharacter)
                {
                    c.ApplyStandardState(new State(StateType.MiasmicSlow, (SpellType + 1) * 12000 / 600), StateType.MiasmicSlowImmunity);
                }

                if (damage == -1)
                {
                    creature.QueueGraphic(Graphic.Create(85, delay, 150));
                }
                else switch (SpellType)
                {
                    case 0:
                        creature.QueueGraphic(Graphic.Create(1847, delay));
                        break;
                    case 1:
                        creature.QueueGraphic(Graphic.Create(1849, delay));
                        break;
                    case 2:
                        creature.QueueGraphic(Graphic.Create(1851, delay));
                        break;
                    case 3:
                        creature.QueueGraphic(Graphic.Create(1854, delay));
                        break;
                }

                caster.QueueTask(new RsTask(() =>
                    {
                        var soak = -1;
                        var dmg = creature.Combat.Attack(caster, DamageType.FullMagic, damage, ref soak);
                        if (dmg != -1)
                        {
                            var splat = new HitSplat(caster);
                            splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= BaseDamage[SpellType]);
                            if (soak != -1)
                            {
                                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                            }

                            creature.QueueHitSplat(splat);
                        }
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
        public bool CheckRequirements(ICharacter caster)
        {
            var hasStaff = false;
            for (var i = 0; i < StaffIds.Length; i++)
            {
                if (caster.Equipment.GetById((short)StaffIds[i]) != null)
                {
                    hasStaff = true;
                    break;
                }
            }

            if (!hasStaff)
            {
                caster.SendChatMessage("You must have Zuriel's staff equiped to cast this spell.");
                return false;
            }

            return caster.Magic.CheckMagicLevel(LevelRequirements[SpellType]) && caster.Magic.CheckRunes(RuneRequirements[SpellType], RuneAmounts[SpellType]);
        }

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(RuneRequirements[SpellType], RuneAmounts[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        /// <param name="activatedOn"></param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, ConfigIds[SpellType]);

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
        public double GetMagicExperience() => Exp[SpellType];
    }
}