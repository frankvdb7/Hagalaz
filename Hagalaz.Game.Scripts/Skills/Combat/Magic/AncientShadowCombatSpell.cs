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
    ///     Class for all shadow blitz,burst,barrage,rush
    /// </summary>
    public class AncientShadowCombatSpell : ICombatSpell
    {
        /// <summary>
        ///     Contains type of the spell.
        /// </summary>
        public int SpellType { get; }

        /// <summary>
        ///     Construct's new ancient shadow combat spell.
        /// </summary>
        public AncientShadowCombatSpell(int spellType) => SpellType = spellType;

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        public static RuneType[][] RuneRequirements = [
            [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul
            ],
            [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        public static int[][] RuneAmounts = [[2, 2, 1, 1], [4, 2, 1, 2], [2, 2, 2, 2], [4, 2, 4, 3]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        public static int[] LevelRequirements = [52, 64, 76, 88];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        public static int[] ConfigIds = [65, 73, 81, 89];

        /// <summary>
        ///     The EXP
        /// </summary>
        public static double[] Exp = [31.0, 37.0, 43.0, 49.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        public static int[] BaseDamage = [140, 180, 240, 280];

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = SpellType == 1 || SpellType == 3;
            caster.QueueAnimation(Animation.Create(multiHit ? 1979 : 1978));
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
                    var projectile = new Projectile(378);
                    projectile.SetSenderData(caster, 43, false);
                    projectile.SetReceiverData(victim, 0);
                    projectile.SetFlyingProperties(51, (short)(vDeltaX * 5 + vDeltaY * 5), 16, 64, false);
                    projectile.Display();
                    break;
                }
                case 2:
                {
                    var projectile = new Projectile(380);
                    projectile.SetSenderData(caster, 43, false);
                    projectile.SetReceiverData(victim, 0);
                    projectile.SetFlyingProperties(51, (short)(vDeltaX * 5 + vDeltaY * 5), 16, 64, false);
                    projectile.Display();
                    break;
                }
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

                if (c is ICharacter target)
                {
                    var percent = SpellType == 3 ? 0.85D : 0.90D;
                    var lowest = (int)(target.Statistics.LevelForExperience(StatisticsConstants.Attack) * percent);
                    var level = target.Statistics.GetSkillLevel(StatisticsConstants.Attack);
                    if (level > lowest)
                    {
                        target.Statistics.DamageSkill(StatisticsConstants.Attack, (byte)(level - lowest));
                    }
                }

                if (damage == -1)
                {
                    creature.QueueGraphic(Graphic.Create(85, delay, 150));
                }
                else switch (SpellType)
                {
                    case 0:
                        creature.QueueGraphic(Graphic.Create(379, delay));
                        break;
                    case 1:
                        creature.QueueGraphic(Graphic.Create(382, delay));
                        break;
                    case 2:
                        creature.QueueGraphic(Graphic.Create(381, delay));
                        break;
                    case 3:
                        creature.QueueGraphic(Graphic.Create(383, delay));
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
                        splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= BaseDamage[SpellType]);
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
        /// <param name="caster"></param>
        /// <returns></returns>
        public bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(LevelRequirements[SpellType]) && caster.Magic.CheckRunes(RuneRequirements[SpellType], RuneAmounts[SpellType]);

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