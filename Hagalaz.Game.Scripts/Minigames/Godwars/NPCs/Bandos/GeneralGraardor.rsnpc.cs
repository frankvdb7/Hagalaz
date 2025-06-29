using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    ///     Contains the general graardor script.
    /// </summary>
    [NpcScriptMetaData([6260])]
    public class GeneralGraardor : General
    {
        private readonly IAudioBuilder _soundBuilder;
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     Initializes the static <see cref="GeneralGraardor" /> class.
        /// </summary>
        static GeneralGraardor()
        {
            _speakData.Add("Death to our enemies!", 3219);
            _speakData.Add("Split their skulls!", 3229);
            _speakData.Add("CHAAARGE!", 3220);
            _speakData.Add("We feast on the bones of our enemies tonight!", 3206);
            _speakData.Add("Crush them underfoot!", 3224);
            _speakData.Add("All glory to Bandos!", 3205);
            _speakData.Add("Brargh!", 3207);
            _speakData.Add("GRAAAAAAAAAR!", 3209);
            _speakData.Add("Break their bones!", 3221);
            _speakData.Add("FOR THE GLORY OF THE BIG HIGH WAR GOD!", 3228);
        }

        public GeneralGraardor(IAudioBuilder soundBuilder, IProjectileBuilder projectileBuilder)
        {
            _soundBuilder = soundBuilder;
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Contains speak data.
        /// </summary>
        private static readonly Dictionary<string, int> _speakData = new();

        /// <summary>
        ///     The attack type
        ///     0 = Melee.
        ///     1 = Ranged.
        /// </summary>
        private int _attackType;

        /// <summary>
        ///     The speak delay.
        /// </summary>
        private int _speakDelay;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attackType)
            {
                case 0:
                    {
                        // Melee attack
                        RenderAttack();
                        var combat = (INpcCombat)Owner.Combat;
                        var damage = combat.GetMeleeDamage(target);
                        var maxDamage = combat.GetMeleeMaxHit(target);
                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = damage, MaxDamage = maxDamage, DamageType = DamageType.StandardMelee, Target = target
                        });
                        break;
                    }
                case 1:
                    {
                        // Ranged attack

                        Owner.QueueAnimation(Animation.Create(7063));

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>()
                            .Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
                        foreach (var c in visibleCharacters)
                        {
                            var deltaX = Owner.Location.X - c.Location.X;
                            var deltaY = Owner.Location.Y - c.Location.Y;
                            if (deltaX < 0)
                            {
                                deltaX = -deltaX;
                            }

                            if (deltaY < 0)
                            {
                                deltaY = -deltaY;
                            }

                            var delay = 25 + deltaX * 2 + deltaY * 2;

                            _projectileBuilder.Create()
                                .WithGraphicId(1200)
                                .FromCreature(Owner)
                                .ToCreature(c)
                                .WithDuration(delay)
                                .WithAngle(180)
                                .WithDelay(40)
                                .Send();

                            var damage = combat.GetRangeDamage(c);
                            var maxDamage = combat.GetRangeMaxHit(c);

                            combat.PerformAttack(new AttackParams()
                            {
                                Damage = damage,
                                MaxDamage = maxDamage,
                                DamageType = DamageType.StandardRange,
                                Target = c,
                                Delay = delay
                            });
                        }

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
            var random = RandomStatic.Generator.NextDouble();
            var rangedChance = 0.25;

            if (!Owner.WithinRange(target, 1))
            {
                rangedChance += 0.075;
            }

            _attackType = random <= rangedChance ? 1 : 0;
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
            switch (_attackType)
            {
                case 0: return 1;
                case 1: return 7;
                default: return base.GetAttackDistance();
            }
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => _attackType == 1 ? AttackBonus.Ranged : AttackBonus.Crush;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => _attackType == 1 ? AttackStyle.RangedAccurate : AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's if this npc can retaliate to specific character attack.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) =>
            Owner.Combat.IsInCombat() && Owner.Combat.RecentAttackers.All(attacker => Owner.Combat.Target == null || attacker.Attacker != Owner.Combat.Target);

        /// <summary>
        ///     Tick's npc.
        /// </summary>
        public override void Tick()
        {
            if (!Owner.Combat.IsInCombat() || Owner.SpeakingText != null)
            {
                return;
            }

            _speakDelay++;
            if (_speakDelay <= 10)
            {
                return;
            }

            _speakDelay = 0;
            foreach (var data in _speakData.Where(data => RandomStatic.Generator.NextDouble() <= 0.008))
            {
                Owner.Speak(data.Key);
                var voice = _soundBuilder.Create().AsVoice().WithId(data.Value).Build();
                voice.PlayWithinDistance(Owner, 8);
                return;
            }
        }
    }
}