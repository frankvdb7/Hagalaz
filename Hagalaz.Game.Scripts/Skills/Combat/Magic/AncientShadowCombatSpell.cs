using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all shadow blitz,burst,barrage,rush
    /// </summary>
    public class AncientShadowCombatSpell : ICombatSpell
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     Contains type of the spell.
        /// </summary>
        public int SpellType { get; }

        public AncientShadowCombatSpell(int spellType, IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
            SpellType = spellType;
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [
                RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul
            ],
            [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts = [[2, 2, 1, 1], [4, 2, 1, 2], [2, 2, 2, 2], [4, 2, 4, 3]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements = [52, 64, 76, 88];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds = [65, 73, 81, 89];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp = [31.0, 37.0, 43.0, 49.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] _baseDamage = [140, 180, 240, 280];

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = SpellType is 1 or 3;
            caster.QueueAnimation(Animation.Create(multiHit ? 1979 : 1978));
            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, _exp[SpellType]);

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

            var duration = vDeltaX * 5 + vDeltaY * 5;

            switch (SpellType)
            {
                case 0:
                    {
                        _projectileBuilder.Create()
                            .WithGraphicId(378)
                            .FromCreature(caster)
                            .ToCreature(victim)
                            .WithDuration(duration)
                            .WithFromHeight(43)
                            .WithAngle(64)
                            .WithSlope(16)
                            .WithDelay(51)
                            .Send();
                        break;
                    }
                case 2:
                    {
                        _projectileBuilder.Create()
                            .WithGraphicId(380)
                            .FromCreature(caster)
                            .ToCreature(victim)
                            .WithDuration(duration)
                            .WithFromHeight(43)
                            .WithAngle(64)
                            .WithSlope(16)
                            .WithDelay(51)
                            .Send();
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

                var maxDamage = combat.GetMagicMaxHit(c, _baseDamage[SpellType]);
                var damage = combat.GetMagicDamage(c, maxDamage);

                var deltaX = caster.Location.X - c.Location.X;
                var deltaY = caster.Location.Y - c.Location.Y;
                if (deltaX < 0)
                {
                    deltaX = -deltaX;
                }

                if (deltaY < 0)
                {
                    deltaY = -deltaY;
                }

                var delay = 51 + deltaX * 5 + deltaY * 5;

                var handle = caster.Combat.PerformAttack(new AttackParams()
                {
                    Damage = damage,
                    MaxDamage = maxDamage,
                    Target = c,
                    Delay = delay,
                    DamageType = DamageType.FullMagic
                });

                handle.RegisterResultHandler(result =>
                {
                    if (result.Damage.Succeeded)
                    {
                        switch (SpellType)
                        {
                            case 0: c.QueueGraphic(Graphic.Create(379)); break;
                            case 1: c.QueueGraphic(Graphic.Create(382)); break;
                            case 2: c.QueueGraphic(Graphic.Create(381)); break;
                            case 3: c.QueueGraphic(Graphic.Create(383)); break;
                        }

                        if (c is not ICharacter charTarget)
                        {
                            return;
                        }

                        var percent = SpellType == 3 ? 0.85D : 0.90D;
                        var lowest = (int)(charTarget.Statistics.LevelForExperience(StatisticsConstants.Attack) * percent);
                        var level = charTarget.Statistics.GetSkillLevel(StatisticsConstants.Attack);
                        if (level > lowest)
                        {
                            charTarget.Statistics.DamageSkill(StatisticsConstants.Attack, level - lowest);
                        }
                    }
                    else
                    {
                        c.QueueGraphic(Graphic.Create(85, height: 150));
                    }
                });
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
        public bool CheckRequirements(ICharacter caster) =>
            caster.Magic.CheckMagicLevel(_levelRequirements[SpellType]) && caster.Magic.CheckRunes(_runeRequirements[SpellType], _runeAmounts[SpellType]);

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runeRequirements[SpellType], _runeAmounts[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        /// <param name="activatedOn"></param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, _configIds[SpellType]);

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
        public double GetMagicExperience() => _exp[SpellType];
    }
}