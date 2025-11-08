using System;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Combat implementation for npcs.
    /// </summary>
    public class NpcCombat : CreatureCombat, INpcCombat
    {
        /// <summary>
        /// The NPC.
        /// </summary>
        private readonly INpc _npc;

        /// <summary>
        /// 
        /// </summary>
        private readonly INpcService _npcService;


        /// <summary>
        /// Construct's new combat class for specified
        /// NPC.
        /// </summary>
        /// <param name="owner"></param>
        public NpcCombat(INpc owner)
            : base(owner)
        {
            _npc = owner;
            _npcService = owner.ServiceProvider.GetRequiredService<INpcService>();
        }

        /// <summary>
        /// Render's npc spawn.
        /// </summary>
        public override void OnSpawn() => IsDead = false;

        /// <summary>
        /// Render's npc death.
        /// </summary>
        public override void OnDeath()
        {
            IsDead = true;
            var delay = RenderDeath();
            Owner.QueueTask(new RsTask(() =>
                {
                    var killer = GetKiller();
                    if (killer != null)
                    {
                        Owner.OnKilledBy(killer);
                        killer.OnTargetKilled(Owner);
                    }

                    ResetAttackers();
                    _npc.Appearance.Visible = false;
                },
                delay));

            if (_npc.Script.CanRespawn())
                Owner.QueueTask(new RsTask(() => _npc.Script.Respawn(), delay + _npc.Definition.RespawnTime + 1));
            else
                Owner.QueueTask(new RsTask(() => _npcService.UnregisterAsync(_npc).Wait(), delay + 1));
        }

        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnTargetKilled(ICreature target) { }

        /// <summary>
        /// This method gets executed on creatures death by creature.
        /// </summary>
        /// <param name="killer">The killer.</param>
        public override void OnKilledBy(ICreature killer)
        {
            if (!CanBeLootedBy(killer))
            {
                return;
            }

            var kill = killer as ICharacter;
            kill?.QueueTask(async () =>
            {
                var lootService = Owner.ServiceProvider.GetRequiredService<ILootService>();
                var table = await lootService.FindNpcLootTable(_npc.Definition.LootTableId);
                if (table == null)
                {
                    return;
                }

                var lootGenerator = Owner.ServiceProvider.GetRequiredService<ILootGenerator>();
                var groundItemBuilder = Owner.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
                foreach (var loot in lootGenerator.GenerateLoot<ILootItem>(new CharacterLootParams(table, kill)))
                {
                    groundItemBuilder.Create()
                        .WithItem(builder => builder.Create().WithId(loot.Item.Id).WithCount(loot.Count))
                        .WithLocation(Owner.Location)
                        .WithOwner(kill)
                        .Spawn();
                }
            });
        }

        /// <summary>
        /// Determines whether this instance [can be looted].
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can be looted]; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanBeLootedBy(ICreature killer) => _npc.Script.CanBeLootedBy(killer);

        /// <summary>
        /// Render's npc death.
        /// </summary>
        /// <returns>Amount of ticks the death gonna be rendered.</returns>
        protected override int RenderDeath() => _npc.Script.RenderDeath();

        /// <summary>
        /// Tick's curses effects.
        /// </summary>
        protected override void CursesTick()
        {
            var attackers = RecentAttackers;
            bool attack = false, defence = false, strength = false, ranged = false, magic = false;
            foreach (var attacker in attackers)
            {
                var att = attacker.Attacker;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.LeechAttack)) attack = true;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.LeechStrength)) strength = true;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.SapRanger) || att.HasState(StateType.SapMager)
                    || att.HasState(StateType.LeechDefence))
                    defence = true;
                if (att.HasState(StateType.SapRanger) || att.HasState(StateType.LeechRanged)) ranged = true;
                if (att.HasState(StateType.SapMager) || att.HasState(StateType.LeechMagic)) magic = true;
            }

            if (!attack && GetPrayerBonus(BonusPrayerType.CurseInstantAttack) != 0)
                _npc.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantAttack, 0);
            if (!strength && GetPrayerBonus(BonusPrayerType.CurseInstantStrength) != 0)
                _npc.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantStrength, 0);
            if (!defence && GetPrayerBonus(BonusPrayerType.CurseInstantDefence) != 0)
                _npc.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantDefence, 0);
            if (!ranged && GetPrayerBonus(BonusPrayerType.CurseInstantRanged) != 0)
                _npc.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantRanged, 0);
            if (!magic && GetPrayerBonus(BonusPrayerType.CurseInstantMagic) != 0)
                _npc.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantMagic, 0);
        }

        /// <summary>
        /// Ticks the attack
        /// </summary>
        protected override void AttackTick()
        {
            if (Target == null)
            {
                return;
            }
            DelayTick = 0;
            Target.Interrupt(this);
            LastAttacked = Target;
            _npc.Script.PerformAttack(Target);
            OnAttackPerformed(Target);
        }

        public int IncomingAttack(ICreature attacker, DamageType damageType, int damage, byte delay) => throw new NotImplementedException();

        /// <summary>
        /// Get's called after attack to specific target.
        /// </summary>
        /// <param name="target"></param>
        public override void OnAttackPerformed(ICreature target)
        {
            if (Owner.HasState(StateType.Turmoil)) _npc.Statistics.SetTurmoilBonuses(target);
            _npc.Script.OnAttackPerformed(target);
        }

        /// <summary>
        /// Get's called after last attacked get's null.
        /// </summary>
        protected override void OnLastAttackedFade()
        {
            if (Owner.HasState(StateType.Turmoil)) _npc.Statistics.ResetTurmoilBonuses();
        }

        /// <summary>
        /// Set's ( Attacks ) the specified target ('target')
        /// </summary>
        /// <param name="target">Creature which should be attacked.</param>
        /// <returns>
        /// If creature target was set sucessfully.
        /// </returns>
        public override bool SetTarget(ICreature target)
        {
            if (!CanSetTarget(target)) return false;
            Target = target;
            Owner.FaceCreature(target);
            _npc.Script.OnSetTarget(target);
            ((Npc)Owner).EventManager.SendEvent(new CreatureSetCombatTargetEvent(Owner, target));
            return true;
        }

        /// <summary>
        /// Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override bool CanSetTarget(ICreature target)
        {
            if (target.IsDestroyed || target.Combat.IsDead || IsDead) return false;
            return _npc.Script.CanSetTarget(target);
        }

        /// <summary>
        /// Get's if this npc can attack specified target.
        /// </summary>
        public override bool CanAttack(ICreature target)
        {
            if (target.IsDestroyed || target.Combat.IsDead || IsDead) return false;
            return _npc.Script.CanAttack(target);
        }

        /// <summary>
        /// Get's if this npc can be attacked by specified attacker.
        /// </summary>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (attacker.IsDestroyed || attacker.Combat.IsDead || IsDead) return false;
            return _npc.Script.CanBeAttackedBy(attacker);
        }

        /// <summary>
        /// Cancel's current target.
        /// </summary>
        public override void CancelTarget()
        {
            Target = null;
            Owner.ResetFacing();
            _npc.Script.OnCancelTarget();
        }

        /// <summary>
        /// Get's attack bonus type of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => _npc.Script.GetAttackBonusType();

        /// <summary>
        /// Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => _npc.Script.GetAttackStyle();

        /// <summary>
        /// Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => _npc.Script.GetAttackDistance();

        /// <summary>
        /// Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => _npc.Script.GetAttackSpeed();

        /// <summary>
        /// Get's specific bonus of this npc.
        /// </summary>
        /// <param name="bonusType"></param>
        /// <returns></returns>
        public override int GetBonus(BonusType bonusType) => _npc.Statistics.Bonuses.GetBonus(bonusType);

        /// <summary>
        /// Get's specific prayer bonus.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns></returns>
        public override int GetPrayerBonus(BonusPrayerType bonusType) => _npc.Statistics.PrayerBonuses.GetBonus(bonusType);

        /// <summary>
        /// Get's attack level of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackLevel()
        {
            double baseLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Attack);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilAttack) * 0.01;
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's strength level of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetStrengthLevel()
        {
            double baseLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Strength);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilStrength) * 0.01;
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's defence level of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetDefenceLevel()
        {
            double baseLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Defence);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilDefence) * 0.01;
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's ranged level of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetRangedLevel()
        {
            double baseLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Ranged);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticRanged) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseRanged) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantRanged) * 0.01;
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's magic level of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetMagicLevel()
        {
            double baseLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Magic);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticMagic) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseMagic) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantMagic) * 0.01;
            return (int)baseLevel;
        }

        /// <summary>
        /// Calculate's magic damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="baseDamage">Base spell maximum damage without bonuses.</param>
        /// <returns>Calculated damage.</returns>
        public int GetMagicDamage(ICreature target, int baseDamage)
        {
            var myAttackLevel = GetMagicLevel();
            var enemyMagicDefenceLevel = target.Combat.GetMagicLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 1;

            double effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);

            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);

            var effectiveMagicDefence = Math.Round((double)enemyMagicDefenceLevel);
            effectiveMagicDefence *= 0.7;
            effectiveDefence *= 0.3;

            effectiveDefence = Math.Round(effectiveDefence);
            effectiveMagicDefence = Math.Round(effectiveMagicDefence);

            effectiveDefence += effectiveMagicDefence;


            double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            var max = GetMagicMaxHit(target, baseDamage);
            return CreatureHelperTwo.PerformMagicDamageCalculation(1, max, accuracy);
        }


        /// <summary>
        /// Calculate's range damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <returns>Calculated damage.</returns>
        public int GetRangeDamage(ICreature target)
        {
            var myAttackLevel = GetRangedLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (GetAttackStyle() == AttackStyle.RangedAccurate) myAttackLevel += 3;
            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 1;

            double effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);


            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);


            double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            var max = GetRangeMaxHit(target);
            return CreatureHelperTwo.PerformRangedDamageCalculation(1, max, accuracy);
        }


        /// <summary>
        /// Calculate's melee damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="maxDamage">The maximum damage.</param>
        /// <returns>
        /// Calculated damage.
        /// </returns>
        public int GetMeleeDamage(ICreature target, int maxDamage)
        {
            var myAttackLevel = GetAttackLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (GetAttackStyle() == AttackStyle.MeleeAccurate)
                myAttackLevel += 3;
            else if (GetAttackStyle() == AttackStyle.MeleeControlled) myAttackLevel++;
            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 1;

            double effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);


            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);


            double attackersRoll = Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            return CreatureHelperTwo.PerformMeleeDamageCalculation(1, maxDamage, accuracy);
        }

        /// <summary>
        /// Calculate's melee damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <returns>
        /// Calculated damage.
        /// </returns>
        public int GetMeleeDamage(ICreature target) => GetMeleeDamage(target, GetMeleeMaxHit(target));

        /// <summary>
        /// Get's melee max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <returns></returns>
        public int GetMeleeMaxHit(ICreature target)
        {
            var myStrengthLevel = GetStrengthLevel();
            var myStrengthBonus = GetBonus(BonusType.Strength);
            var style = GetAttackStyle();
            switch (style)
            {
                case AttackStyle.MeleeAggressive: myStrengthLevel += 3; break;
                case AttackStyle.MeleeControlled: myStrengthLevel += 1; break;
            }

            var maxHitModifier = 1.0;
            if (Owner.HasState(StateType.DharokWretchedStrength))
            {
                if (RandomStatic.Generator.NextDouble() >= 0.50)
                {
                    var hp = _npc.Statistics.LifePoints;
                    var maxhp = _npc.Definition.MaxLifePoints;
                    var d = hp / maxhp;
                    maxHitModifier = 2.0 - d;
                }
            }

            var baseDamage = 13.0 + myStrengthLevel + myStrengthBonus / 8.0 + myStrengthLevel * (double)myStrengthBonus / 64.0;
            if (maxHitModifier > 1.0) return (int)Math.Floor(baseDamage * maxHitModifier);
            return (int)Math.Truncate(baseDamage);
        }

        /// <summary>
        /// Get's range max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <returns></returns>
        public int GetRangeMaxHit(ICreature target)
        {
            var myRangedLevel = GetRangedLevel();
            if (GetAttackStyle() == AttackStyle.RangedAccurate) myRangedLevel += 3;
            var myRangedStrength = GetBonus(BonusType.RangedStrength);
            var effectiveStrength = Math.Floor((double)(myRangedLevel + myRangedStrength));
            var baseDamage = 5.0 + (effectiveStrength + 8.0) * (myRangedStrength + 64.0) / 64.0;
            return (int)Math.Floor(baseDamage);
        }

        /// <summary>
        /// Get's magic max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="baseDamage">Contains base magic damage the spell does.</param>
        /// <returns></returns>
        public int GetMagicMaxHit(ICreature target, int baseDamage)
        {
            var myMagicLevel = _npc.Statistics.GetSkillLevel(NpcStatisticsConstants.Magic);
            var myMaxMagicLevel = _npc.Statistics.GetMaxSkillLevel(NpcStatisticsConstants.Magic);
            var myMagicDamageBonus = GetBonus(BonusType.MagicDamage);
            double damage = baseDamage;
            for (var i = myMaxMagicLevel + 1; i <= myMagicLevel; i++) damage *= 1.03;
            damage *= 1.0 + myMagicDamageBonus / 100.0;
            return (int)Math.Truncate(damage);
        }

        /// <summary>
        /// Perform's incomming attack on this npc.
        /// This attack does not damage the target but does check for protection prayers and performs
        /// animations.
        /// Attacker target must be this npc.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker creature.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach target.</param>
        /// <returns>Returns amount of damage that should be dealed.</returns>
        public override int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            if (IsDead) return -1;

            damage = _npc.Script.OnIncomingAttack(attacker, damageType, damage, delay);
            if (damageType != DamageType.Reflected) _npc.Script.RenderDefence(delay);
            if (damage <= 0) return -1;
            return damage;
        }


        /// <summary>
        /// Perform's attack on this npc.
        /// Attacker target must be this npc.
        /// Returns amount of damage that should be rendered on hitsplat.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker creature.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="damageSoaked">Variable which will be set to amount of damage that was soaked.
        /// If no damage was soaked then the variable will be -1.</param>
        /// <returns>Returns amount of damage that should be rendered on hitsplat.</returns>
        public override int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked)
        {
            if (IsDead) return -1;

            AddAttacker(attacker);

            damage = _npc.Script.OnAttack(attacker, damageType, damage);

            damageSoaked = -1;
            double soakPercent = 0;
            switch (damageType)
            {
                case DamageType.FullMelee:
                case DamageType.StandardMelee:
                    soakPercent = GetBonus(BonusType.AbsorbMelee) * 0.01;
                    break;
                case DamageType.FullRange:
                case DamageType.StandardRange:
                    soakPercent = GetBonus(BonusType.AbsorbRange) * 0.01;
                    break;
                case DamageType.FullMagic:
                case DamageType.StandardMagic:
                    soakPercent = GetBonus(BonusType.AbsorbMagic) * 0.01;
                    break;
            }

            if (damage > 200)
            {
                var soak = (int)(damage * soakPercent);
                if (soak > 0)
                {
                    damageSoaked = soak;
                    damage -= soak;
                }
            }

            if (damage != -1)
            {
                damage = _npc.Statistics.DamageLifePoints(damage);
                AddDamageToAttacker(attacker, damage);
            }

            if (_npc.Script.CanRetaliateTo(attacker)) Owner.QueueTask(new RsTask(() => Owner.Combat.SetTarget(attacker), 1));
            return damage;
        }
    }
}