using System;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events;
using Hagalaz.Services.GameWorld.Model.Creatures.Combat;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Class for npc statistics.
    /// </summary>
    public class NpcStatistics : INpcStatistics
    {
        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly Npc _owner;

        /// <summary>
        /// Contains skills data of this npc.
        /// </summary>
        private readonly int[] _skillData = new int[NpcStatisticsConstants.SkillsCount];

        /// <summary>
        /// The combat level
        /// </summary>
        private int _combatLevel;

        /// <summary>
        /// Contains npc combat level.
        /// </summary>
        /// <value>The combat level.</value>
        public int CombatLevel
        {
            get => _combatLevel;
            set
            {
                if (CombatLevel != value)
                {
                    _combatLevel = value;
                    _owner.RenderInformation.ScheduleFlagUpdate(UpdateFlags.SetCombatLevel);
                }
            }
        }

        /// <summary>
        /// Contains npc hitpoints.
        /// </summary>
        /// <value>The hit points.</value>
        public int LifePoints { get; private set; }

        /// <summary>
        /// Contains npc bonuses.
        /// </summary>
        /// <value>The bonuses.</value>
        public IBonuses Bonuses { get; }

        /// <summary>
        /// Contains npc prayer bonuses.
        /// </summary>
        /// <value>The prayer bonuses.</value>
        public IBonusesPrayer PrayerBonuses { get; }

        /// <summary>
        /// Contains npc poison amount.
        /// </summary>
        /// <value>The poison amount.</value>
        public int PoisonAmount { get; private set; }

        /// <summary>
        /// Get's if npc is poisoned.
        /// </summary>
        /// <value><c>true</c> if poisoned; otherwise, <c>false</c>.</value>
        public bool Poisoned => PoisonAmount > 0;

        /// <summary>
        /// Contains hitpoints restore tick, when this get's
        /// higher than hitpoints restore rate , the npc hitpoints gets restored by 1.
        /// </summary>
        private int _lifePointsRestoreTick;

        /// <summary>
        /// Contains hitpoints restore tick, when this get's
        /// higher than hitpoints restore rate , the npc hitpoints gets normalized by 1.
        /// </summary>
        private int _lifePointsNormalizeTick;

        /// <summary>
        /// Contains poison tick.
        /// </summary>
        private int _poisonTick;

        /// <summary>
        /// Contains prayer bonuses tick.
        /// </summary>
        private int _prayerBonusesTick;

        /// <summary>
        /// Contains statistics restore tick.
        /// </summary>
        private int _statisticsRestoreTick;

        /// <summary>
        /// Construct's new statistics class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public NpcStatistics(Npc owner)
        {
            _owner = owner;
            CombatLevel = _owner.Definition.CombatLevel;
            LifePoints = _owner.Definition.MaxLifePoints;
            Bonuses = _owner.Definition.Bonuses.Copy();
            PrayerBonuses = new PrayerBonuses();
            for (var i = 0; i < NpcStatisticsConstants.SkillsCount; i++) _skillData[i] = GetMaxSkillLevel(i);
        }

        /// <summary>
        /// Set's npc hitpoints.
        /// If hp is 0 or lower then npc.OnDeath() get's called.
        /// </summary>
        /// <param name="hp">The hp.</param>
        public void SetLifePoints(int hp)
        {
            LifePoints = (short)hp;
            if (hp <= 0) _owner.OnDeath();
        }

        /// <summary>
        /// Damage's npc hitpoints by the given amount.
        /// npc.OnDeath() is possible.
        /// </summary>
        /// <param name="damage">Amount to damage hitpoints.</param>
        /// <returns>The amount of points actually damaged.</returns>
        public int DamageLifePoints(int damage)
        {
            if (damage > LifePoints) damage = LifePoints;
            var last = LifePoints;
            SetLifePoints(LifePoints - damage);
            _owner.QueueHitBar(new HitBar(HitBarType.Regular, last, LifePoints, 0, 0));
            return damage;
        }

        /// <summary>
        /// Heal's npc hitpoints by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal hitpoints.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        public int HealLifePoints(int amount)
        {
            if (LifePoints + amount > _owner.Definition.MaxLifePoints) amount = _owner.Definition.MaxLifePoints - LifePoints;
            if (amount < 0) amount = 0;
            SetLifePoints(LifePoints + amount);
            return amount;
        }

        /// <summary>
        /// Damage's (Decreases) specific skill.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        /// <param name="damage">The damage.</param>
        /// <returns>Returns the actual damage.</returns>
        public int DamageSkill(int skillID, int damage)
        {
            if (damage > _skillData[skillID]) damage = _skillData[skillID];
            _skillData[skillID] -= damage;
            return damage;
        }

        /// <summary>
        /// Heals (Increases) specific skill.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>Returns the actual heal amount.</returns>
        public int HealSkill(int skillID, int amount)
        {
            if (_skillData[skillID] + amount > GetMaxSkillLevel(skillID)) amount = GetMaxSkillLevel(skillID) - amount;
            if (amount < 0) amount = 0;
            _skillData[skillID] += amount;
            return amount;
        }

        /// <summary>
        /// Get's max level of specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception"></exception>
        public int GetMaxSkillLevel(int skillID) =>
            skillID switch
            {
                NpcStatisticsConstants.Attack => _owner.Definition.MaxAttackLevel,
                NpcStatisticsConstants.Defence => _owner.Definition.MaxDefenceLevel,
                NpcStatisticsConstants.Strength => _owner.Definition.MaxStrengthLevel,
                NpcStatisticsConstants.Ranged => _owner.Definition.MaxRangedLevel,
                NpcStatisticsConstants.Magic => _owner.Definition.MaxMagicLevel,
                _ => throw new Exception("Bad SkillID!"),
            };

        /// <summary>
        /// Increase's dynamic prayer bonus of specific type by 1.
        /// Does not work if current prayer bonus is higher or equal to 15.
        /// Type must be curse bonus!
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="max">The max.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool IncreaseCursePrayerBonus(BonusPrayerType type, int max)
        {
            if (PrayerBonuses.GetBonus(type) >= max) return false;
            PrayerBonuses.SetBonus(type, PrayerBonuses.GetBonus(type) + 1);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
            return true;
        }

        /// <summary>
        /// Decrease's dynamic prayer bonus of specific type by 1.
        /// Does not work if current bonus is lower or equal to -25.
        /// Type must be curse bonus!
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="max">The max.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool DecreaseCursePrayerBonus(BonusPrayerType type, int max)
        {
            if (PrayerBonuses.GetBonus(type) <= max) return false;
            PrayerBonuses.SetBonus(type, PrayerBonuses.GetBonus(type) - 1);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
            return true;
        }

        /// <summary>
        /// Reset's all dynamic prayer bonuses.
        /// Does not send the 'Your skill_name is not affected by sap/leech curses' message.
        /// </summary>
        public void ResetCursePrayerBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.CurseAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseDefence, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseRanged, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseMagic, 0);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Reset's all instant prayer bonuses.
        /// </summary>
        public void ResetInstantCursePrayerBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantDefence, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantRanged, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantMagic, 0);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Set's instant curse prayer bonus.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        public void SetInstantCursePrayerBonus(BonusPrayerType type, int value)
        {
            PrayerBonuses.SetBonus(type, value);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Get's level of specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception"></exception>
        public int GetSkillLevel(int skillID)
        {
            if (skillID >= NpcStatisticsConstants.SkillsCount) throw new Exception("Bad SkillID!");
            return _skillData[skillID];
        }

        /// <summary>
        /// Normalises this npc statistics,
        /// sets hitpoints to maximum.
        /// </summary>
        public void Normalise()
        {
            LifePoints = _owner.Definition.MaxLifePoints;
            for (var i = 0; i < NpcStatisticsConstants.SkillsCount; i++) _skillData[i] = GetMaxSkillLevel(i);
            ResetCursePrayerBonuses();
            ResetInstantCursePrayerBonuses();
        }

        /// <summary>
        /// Get's the npc hitpoints restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetLifePointsRestoreRate() => 10;

        /// <summary>
        /// Get's npc hitpoints normalize rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetLifePointsNormalizeRate() => 10;

        /// <summary>
        /// Get's statistics restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetStatisticsRestoreRate() => 100;

        /// <summary>
        /// Get's character poison rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetPoisonRate() => 30;


        /// <summary>
        /// Set's character poison amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        public void SetPoisonAmount(int amount) => PoisonAmount = amount;

        /// <summary>
        /// Set's turmoil bonuses according to target's skills.
        /// </summary>
        /// <param name="target">The target.</param>
        public void SetTurmoilBonuses(ICreature target)
        {
            int attackBonusLevels = (int)(target.Combat.GetAttackLevel() * 0.15);
            int defenceBonusLevels = (int)(target.Combat.GetDefenceLevel() * 0.15);
            int strengthBonusLevels = (int)(target.Combat.GetStrengthLevel() * 0.10);

            int percentAttack = (int)(attackBonusLevels / (double)GetSkillLevel(NpcStatisticsConstants.Attack) * 100.0);
            int percentDefence = (int)(defenceBonusLevels / (double)GetSkillLevel(NpcStatisticsConstants.Defence) * 100.0);
            int percentStrength = (int)(strengthBonusLevels / (double)GetSkillLevel(NpcStatisticsConstants.Strength) * 100.0);

            if (percentAttack > 15) percentAttack = 15;
            if (percentDefence > 15) percentDefence = 15;
            if (percentStrength > 15) percentStrength = 15;

            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilAttack, percentAttack);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilStrength, percentStrength);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilDefence, percentDefence);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Reset's all turmoil bonuses.
        /// </summary>
        public void ResetTurmoilBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilDefence, 0);
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }


        /// <summary>
        /// Tick's npc statistics.
        /// </summary>
        public void Tick()
        {
            // hitpoints tick
            if (_lifePointsRestoreTick++ > GetLifePointsRestoreRate())
            {
                _lifePointsRestoreTick = 0;
                if (LifePoints < _owner.Definition.MaxLifePoints) HealLifePoints(1);
            }

            // hitpoints normalize tick
            if (_lifePointsNormalizeTick++ > GetLifePointsNormalizeRate())
            {
                _lifePointsNormalizeTick = 0;
                if (LifePoints > _owner.Definition.MaxLifePoints) DamageLifePoints(1);
            }

            // standart statistics.
            if (_statisticsRestoreTick++ > GetStatisticsRestoreRate())
            {
                _statisticsRestoreTick = 0;
                for (var i = 0; i < NpcStatisticsConstants.SkillsCount; i++)
                {
                    var max = GetMaxSkillLevel(i);
                    if (_skillData[i] < max)
                        HealSkill(i, 1);
                    else if (_skillData[i] > max) DamageSkill(i, 1);
                }
            }

            // poison tick
            if (_poisonTick++ > GetPoisonRate() && !_owner.Combat.IsDead)
            {
                _poisonTick = 0;
                if (PoisonAmount >= 10)
                {
                    var amt = DamageLifePoints(PoisonAmount);
                    var splat = _owner.ServiceProvider.GetRequiredService<IHitSplatBuilder>()
                        .Create()
                        .AddSprite(builder => builder.WithDamage(amt).WithSplatType(HitSplatType.HitPoisonDamage))
                        .Build();
                    _owner.QueueHitSplat(splat);
                    SetPoisonAmount(PoisonAmount - 2 > 0 ? PoisonAmount - 2 : 0);
                }
                else
                    SetPoisonAmount(0);
            }

            // prayer bonus tick
            if (_prayerBonusesTick++ > 50)
            {
                _prayerBonusesTick = 0;
                for (int i = 5; i < 10; i++)
                {
                    int bonus = PrayerBonuses.GetBonus((BonusPrayerType)i);
                    if (bonus < 0)
                        IncreaseCursePrayerBonus((BonusPrayerType)i, 0);
                    else if (bonus > 0) DecreaseCursePrayerBonus((BonusPrayerType)i, 0);
                }
            }
        }
    }
}