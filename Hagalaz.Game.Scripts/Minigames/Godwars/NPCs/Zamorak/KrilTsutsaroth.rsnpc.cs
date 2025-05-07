using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak
{
    /// <summary>
    /// </summary>
    public class KrilTsutsaroth : General
    {
        private readonly IAudioBuilder _soundBuilder;

        /// <summary>
        ///     Initializes the <see cref="KrilTsutsaroth" /> class.
        /// </summary>
        static KrilTsutsaroth()
        {
            _speakData.Add("Attack them, you dogs!", 3278);
            _speakData.Add("Forward!", 3276);
            _speakData.Add("Death to Saradomin's dogs!", 3277);
            _speakData.Add("Kill them, you cowards!", 3290);
            _speakData.Add("The Dark One will have their souls!", 3280);
            _speakData.Add("Zamorak curse them!", 3270);
            //SPEAK_DATA.Add("YARRRRRRR!", 3274); // special attack
            _speakData.Add("Rend them limb from limb!", 3273);
            // SPEAK_DATA.Add("No retreat!", 3258);
            _speakData.Add("Slay them all!", 3279);
            _speakData.Add("Attack!", 3282);
        }

        public KrilTsutsaroth(IAudioBuilder soundBuilder) => _soundBuilder = soundBuilder;

        /// <summary>
        ///     Contains speak data.
        /// </summary>
        private static readonly Dictionary<string, short> _speakData = new Dictionary<string, short>();

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
            GenerateAttackType(target);
            switch (_attackType)
            {
                // melee
                case 0:
                    base.PerformAttack(target);
                    break;
                // magic
                case 1:
                    {
                        Owner.QueueAnimation(Animation.Create(14384));
                        Owner.QueueGraphic(Graphic.Create(1210));

                        const int maxHit = 300;

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && CanBeAttackedBy(Owner));
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

                            var delay = (byte)(25 + deltaX * 2 + deltaY * 2);

                            var projectile = new Projectile(1211);
                            projectile.SetSenderData(Owner, 0);
                            projectile.SetReceiverData(c, 0);
                            projectile.SetFlyingProperties(40, delay, 0, 180, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(c, maxHit);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

                            var toAttack = c;
                            Owner.QueueTask(new RsTask(() =>
                            {
                                var soak = -1;
                                var damage = toAttack.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                                var splat = new HitSplat(Owner);
                                splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(toAttack, maxHit) <= damage);
                                if (soak != -1)
                                {
                                    splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                }

                                toAttack.QueueHitSplat(splat);
                            }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
                // special
                case 2:
                    {
                        const int maxDamage = 497;
                        Owner.Speak("YARRRRRRR!");
                        _soundBuilder.Create().AsVoice().WithId(3274).Build().PlayWithinDistance(Owner, 8);

                        RenderAttack();
                        var preDmg = target.Combat.IncomingAttack(Owner, DamageType.Reflected, ((INpcCombat)Owner.Combat).GetMeleeDamage(target, maxDamage), 0);
                        var soaked = -1;
                        var damage = target.Combat.Attack(Owner, DamageType.StandardMelee, preDmg, ref soaked);
                        var splat = new HitSplat(Owner);
                        splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, maxDamage <= damage);
                        if (soaked != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                        }

                        target.QueueHitSplat(splat);
                        if (target is ICharacter character)
                        {
                            character.SendChatMessage("K'ril Tsutsaroth slams through your protection prayer, leaving you feeling drained.");
                        }

                        break;
                    }
            }
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature target) => _attackType = RandomStatic.Generator.Next(0, 3);

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_attackType)
            {
                case 0:
                    return AttackStyle.MeleeAggressive;
                case 1:
                    return AttackStyle.MagicNormal;
                case 2:
                    return AttackStyle.MeleeAggressive;
                default:
                    return base.GetAttackStyle();
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
            switch (_attackType)
            {
                case 0:
                    return AttackBonus.Slash;
                case 1:
                    return AttackBonus.Magic;
                case 2:
                    return AttackBonus.Crush;
                default:
                    return base.GetAttackBonusType();
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
            switch (_attackType)
            {
                case 0:
                    return 1;
                case 1:
                    return 8;
                case 2:
                    return 1;
                default:
                    return base.GetAttackDistance();
            }
        }

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
                _soundBuilder.Create().AsVoice().WithId(data.Value).Build().PlayWithinDistance(Owner, 8);
                return;
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() =>
        [
            6203
        ];
    }
}