using System;
using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Combat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using static Hagalaz.Game.Abstractions.Model.Creatures.Characters.StatisticsConstants;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class Statistics
    /// </summary>
    public class CharacterStatistics : ICharacterStatistics, IHydratable<HydratedStatisticsDto>, IDehydratable<HydratedStatisticsDto>
    {
        /// <summary>
        /// Represents a single skill.
        /// </summary>
        private struct Skill
        {
            /// <summary>
            /// The skill's level.
            /// </summary>
            /// <value>The level.</value>
            public int Level { get; set; }

            /// <summary>
            /// The skill's experience.
            /// </summary>
            /// <value>The experience.</value>
            public double Experience { get; set; }

            /// <summary>
            /// Contains the target level.
            /// </summary>
            public int TargetLevel { get; set; }

            /// <summary>
            /// Contains the target experience.
            /// </summary>
            public double TargetExperience { get; set; }
        }

        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// The array of skills holding character's skill data.
        /// </summary>
        private readonly Skill[] _skillData = new Skill[SkillsCount];

        /// <summary>
        /// The array of skills holding character's previous skill data.
        /// </summary>
        private readonly Skill[] _previousSkillData = new Skill[SkillsCount];

        /// <summary>
        /// Contains the xp counters.
        /// </summary>
        private int[] _xpCounters = new int[3];

        /// <summary>
        /// Contains tracked xp counter ids.
        /// </summary>
        private int[] _trackedXpCounters = new int[3];

        /// <summary>
        /// Contains enabled xp counters.
        /// </summary>
        private bool[] _enabledXpCounters = new bool[3];

        private readonly IOptions<CombatOptions> _combatOptions;

        private readonly IOptions<SkillOptions> _skillOptions;

        /// <summary>
        /// Gets the character's combat level. (Including summoning)
        /// </summary>
        /// <value>The full combat level.</value>
        public int FullCombatLevel
        {
            get
            {
                int attack = LevelForExperience(Attack);
                int defence = LevelForExperience(Defence);
                int strength = LevelForExperience(Strength);
                int hp = LevelForExperience(Constitution);
                int prayer = LevelForExperience(Prayer);
                int ranged = LevelForExperience(Ranged);
                int magic = LevelForExperience(StatisticsConstants.Magic);
                var melee = (attack + strength) * 0.325;
                var ranger = Math.Floor(ranged * 1.5) * 0.325;
                var mage = Math.Floor(magic * 1.5) * 0.325;
                var combatLevel = (defence + hp + Math.Floor(prayer / 2.0)) * 0.25 + 1;

                if (melee >= ranger && melee >= mage)
                    combatLevel += (int)melee;
                else if (ranger >= melee && ranger >= mage)
                    combatLevel += (int)ranger;
                else if (mage >= melee && mage >= ranger) combatLevel += (int)mage;
                return (int)(combatLevel + LevelForExperience(Summoning) / 8.0);
            }
        }

        /// <summary>
        /// Gets the character's combat level. (Excluding summoning)
        /// </summary>
        /// <value>The base combat level.</value>
        public int BaseCombatLevel
        {
            get
            {
                int attack = LevelForExperience(Attack);
                int defence = LevelForExperience(Defence);
                int strength = LevelForExperience(Strength);
                int hp = LevelForExperience(Constitution);
                int prayer = LevelForExperience(Prayer);
                int ranged = LevelForExperience(Ranged);
                int magic = LevelForExperience(StatisticsConstants.Magic);
                var melee = (attack + strength) * 0.325;
                var ranger = Math.Floor(ranged * 1.5) * 0.325;
                var mage = Math.Floor(magic * 1.5) * 0.325;
                var combatLevel = (defence + hp + Math.Floor(prayer / 2.0)) * 0.25 + 1;

                if (melee >= ranger && melee >= mage)
                    combatLevel += (int)melee;
                else if (ranger >= melee && ranger >= mage)
                    combatLevel += (int)ranger;
                else if (mage >= melee && mage >= ranger) combatLevel += (int)mage;
                return (int)combatLevel;
            }
        }

        /// <summary>
        /// Get's character's total level.
        /// </summary>
        /// <value>The total level.</value>
        public int TotalLevel
        {
            get
            {
                var total = 0;
                for (var i = 0; i < SkillsCount; i++) total += LevelForExperience(i);
                return total;
            }
        }

        /// <summary>
        /// Get's if player is poisoned.
        /// </summary>
        /// <value><c>true</c> if poisoned; otherwise, <c>false</c>.</value>
        public bool Poisoned => PoisonAmount > 0;

        /// <summary>
        /// Contains character hitpoints.
        /// </summary>
        /// <value>The constitution points.</value>
        public int LifePoints { get; private set; }

        /// <summary>
        /// Contains character prayer points.
        /// </summary>
        /// <value>The prayer points.</value>
        public int PrayerPoints { get; private set; }

        /// <summary>
        /// Contains character run energy.
        /// </summary>
        /// <value>The run energy.</value>
        public int RunEnergy { get; private set; }

        /// <summary>
        /// Contains character special energy.
        /// </summary>
        /// <value>The special energy.</value>
        public int SpecialEnergy { get; private set; }

        /// <summary>
        /// Contains character bonuses.
        /// </summary>
        /// <value>The bonuses.</value>
        public IBonuses Bonuses { get; }

        /// <summary>
        /// Contains character prayer bonuses.
        /// </summary>
        /// <value>The prayer bonuses.</value>
        public IBonusesPrayer PrayerBonuses { get; }

        /// <summary>
        /// Contains flashed skills flag.
        /// </summary>
        /// <value>The skills flash flag.</value>
        public int SkillsFlashFlag { get; private set; }

        /// <summary>
        /// Contains character poison amount.
        /// </summary>
        /// <value>The poison amount.</value>
        public int PoisonAmount { get; private set; }

        /// <summary>
        /// Çontains the character total play time.
        /// </summary>
        public TimeSpan PlayTime { get; private set; }

        /// <summary>
        /// Contains hitpoints restore tick, when this get's
        /// higher than hitpoints restore rate , the character hitpoints gets restored by 1.
        /// </summary>
        private int _lifePointsRestoreTick;

        /// <summary>
        /// Contains hitpoints restore tick, when this get's
        /// higher than hitpoints restore rate , the character hitpoints gets normalized by 1.
        /// </summary>
        private int _lifePointsNormalizeTick;

        /// <summary>
        /// Contains special energy restore tick, when this get's
        /// higher than special energy restore rate, the character special energy gets restored by 100.
        /// </summary>
        private int _specialEnergyRestoreTick;

        /// <summary>
        /// Contains character poison tick.
        /// </summary>
        private int _poisonTick;

        /// <summary>
        /// Contains various prayer bonuses tick.
        /// </summary>
        private int _prayerBonusesTick;

        /// <summary>
        /// Contains various statistics restore tick.
        /// </summary>
        private int _statisticsRestoreTick;

        /// <summary>
        /// Contains various statistics normalize tick.
        /// </summary>
        private int _statisticsNormalizeTick;

        /// <summary>
        /// Contains the run energy restore status.
        /// </summary>
        private int _runEnergyRestoreStatus;

        /// <summary>
        /// Contains the run energy drain status.
        /// </summary>
        private int _runEnergyDrainStatus;

        /// <summary>
        /// Constructs new statistics class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public CharacterStatistics(ICharacter owner)
        {
            _owner = owner;
            Bonuses = new Bonuses();
            PrayerBonuses = new PrayerBonuses();
            for (var i = 0; i < SkillsCount; i++)
                _skillData[i] = new Skill
                {
                    Experience = 0, Level = 1, TargetExperience = -1, TargetLevel = -1
                };
            for (var i = 0; i < SkillsCount; i++)
                _previousSkillData[i] = new Skill
                {
                    Experience = 0, Level = 1, TargetExperience = -1, TargetLevel = -1
                };
            _skillData[Constitution].Level = 10;
            _skillData[Constitution].Experience = 1184;
            LifePoints = 100;
            PrayerPoints = 10;
            RunEnergy = MaximumRunEnergy;
            SpecialEnergy = MaximumSpecialEnergy;

            _combatOptions = owner.ServiceProvider.GetRequiredService<IOptions<CombatOptions>>();
            _skillOptions = owner.ServiceProvider.GetRequiredService<IOptions<SkillOptions>>();
        }

        /// <summary>
        /// Gets the level for the amount of exp for the specified skill.
        /// </summary>
        /// <param name="skillID">The skill</param>
        /// <returns>System.Byte.</returns>
        public int LevelForExperience(int skillID)
        {
            double points = 0;

            var max = skillID == Dungeoneering ? 120 : 99;

            for (var lvl = 1; lvl < max + 1; lvl++)
            {
                points += Math.Floor(lvl + 300.0 * Math.Pow(2.0, lvl / 7.0));
                var output = (int)Math.Floor(points / 4);
                if (output - 1 >= _skillData[skillID].Experience) return lvl;
            }

            return max;
        }

        /// <summary>
        /// Get's skill level.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="Exception"></exception>
        public int GetSkillLevel(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            return _skillData[skillID].Level;
        }

        /// <summary>
        /// Get's skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception"></exception>
        public double GetSkillExperience(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            return _skillData[skillID].Experience;
        }

        /// <summary>
        /// Get's previous skill level.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="Exception"></exception>
        public int GetPreviousSkillLevel(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            return _previousSkillData[skillID].Level;
        }

        /// <summary>
        /// Get's previous skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception"></exception>
        public double GetPreviousSkillExperience(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            return _previousSkillData[skillID].Experience;
        }

        /// <summary>
        /// Set's skill level.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="level">Skill level.</param>
        /// <exception cref="Exception">Bad skillID</exception>
        public void SetSkillLevel(int skillID, int level)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            _previousSkillData[skillID].Level = _skillData[skillID].Level;
            if (_skillData[skillID].Level == level)
            {
                return;
            }

            _skillData[skillID].Level = level;
            RefreshSkill(skillID);
        }

        /// <summary>
        /// Set's skill experience.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="experience">Skill experience.</param>
        /// <exception cref="Exception"></exception>
        public void SetSkillExperience(int skillID, double experience)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");

            var checkCombat = skillID <= StatisticsConstants.Magic || skillID == Summoning;
            var previousCombatLevel = 0;
            if (checkCombat) previousCombatLevel = FullCombatLevel;

            _previousSkillData[skillID].Experience = _skillData[skillID].Experience;
            _skillData[skillID].Experience = experience;

            var currentCombatLevel = 0;
            if (checkCombat) currentCombatLevel = FullCombatLevel;

            if (previousCombatLevel != currentCombatLevel) _owner.Appearance.Refresh();

            if (_skillData[skillID].Experience.CompareTo(_previousSkillData[skillID].Experience) == 0)
            {
                return;
            }

            RefreshSkill(skillID);
            _owner.OnSkillExperienceGain(skillID, experience);
        }

        /// <summary>
        /// Sets the skill target level.
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="level">The level.</param>
        /// <exception cref="Exception">Bad skillID</exception>
        public void SetSkillTargetLevel(int skillID, int level)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            if (level > (skillID == 24 ? 120 : 99)) return;
            _previousSkillData[skillID].TargetLevel = _skillData[skillID].TargetLevel;
            _previousSkillData[skillID].TargetExperience = _skillData[skillID].TargetExperience;
            if (_skillData[skillID].TargetLevel != level)
            {
                _skillData[skillID].TargetLevel = level;
                _skillData[skillID].TargetExperience = -1;
                RefreshSkillTarget(skillID);
            }
        }

        /// <summary>
        /// Sets the skill target experience.
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="experience">The experience.</param>
        /// <exception cref="Exception">Bad skillID</exception>
        public void SetSkillTargetExperience(int skillID, int experience)
        {
            if (skillID >= SkillsCount) throw new Exception("Bad skillID");
            if (experience > MaximumExperience) return;
            _previousSkillData[skillID].TargetLevel = _skillData[skillID].TargetLevel;
            _previousSkillData[skillID].TargetExperience = _skillData[skillID].TargetExperience;
            if (_skillData[skillID].TargetExperience != experience)
            {
                _skillData[skillID].TargetLevel = -1;
                _skillData[skillID].TargetExperience = experience;
                RefreshSkillTarget(skillID);
            }
        }

        /// <summary>
        /// Refreshe's client with current data.
        /// </summary>
        public void Refresh()
        {
            RefreshSkills();
            RefreshXpCounters();
            RefreshLifePoints();
            RefreshPrayerPoints();
            RefreshRunEnergy();
            RefreshSpecialEnergy();
            RefreshFlashedSkills();
            RefreshPoison();
        }

        /// <summary>
        /// Set's character special energy.
        /// </summary>
        /// <param name="amount">The amount.</param>
        public void SetSpecialEnergy(int amount)
        {
            if (SpecialEnergy != amount)
            {
                SpecialEnergy = amount;
                RefreshSpecialEnergy();
            }
        }

        /// <summary>
        /// Refreshe's character special energy.
        /// </summary>
        public void RefreshSpecialEnergy() => _owner.Configurations.SendStandardConfiguration(300, SpecialEnergy);

        /// <summary>
        /// Refreshes the enabled xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <exception cref="Exception">Invalid counter Id value.</exception>
        public void RefreshEnabledXpCounter(int counterID)
        {
            if (counterID >= _xpCounters.Length) throw new Exception("Invalid counter Id value.");
            _owner.Configurations.SendBitConfiguration(10444 + counterID, _enabledXpCounters[counterID] ? 1 : 0);
        }

        /// <summary>
        /// Refreshes the tracked xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <exception cref="Exception">Invalid counter Id value.</exception>
        public void RefreshTrackedXpCounter(int counterID)
        {
            if (counterID >= _xpCounters.Length) throw new Exception("Invalid counter Id value.");
            _owner.Configurations.SendBitConfiguration(10440 + counterID, _trackedXpCounters[counterID] + 1);
        }

        /// <summary>
        /// Refreshes the xp counters, including the refresh of
        /// -Enabled XP counters
        /// -Tracked XP counters
        /// -XP Counter value
        /// </summary>
        public void RefreshXpCounters()
        {
            for (var id = 0; id < _xpCounters.Length; id++)
            {
                RefreshEnabledXpCounter(id);
                RefreshTrackedXpCounter(id);
                RefreshXpCounter(id);
            }
        }

        /// <summary>
        /// Refreshe's xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <exception cref="Exception">Invalid counter Id value.</exception>
        public void RefreshXpCounter(int counterID)
        {
            if (counterID >= _xpCounters.Length) throw new Exception("Invalid counter Id value.");
            _owner.Configurations.SendStandardConfiguration(counterID == 0 ? 1801 : 2474 + counterID, _xpCounters[counterID] * 10);
        }

        /// <summary>
        /// Toggles the xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <exception cref="Exception">Invalid counter Id value.</exception>
        public void ToggleXpCounter(int counterID)
        {
            if (counterID >= _xpCounters.Length) throw new Exception("Invalid counter Id value.");
            _enabledXpCounters[counterID] = !_enabledXpCounters[counterID];
            RefreshEnabledXpCounter(counterID);
        }

        /// <summary>
        /// Sets the tracked xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <param name="optionID">The skill identifier.</param>
        /// <exception cref="Exception">Invalid Id given.</exception>
        public void SetTrackedXpCounter(int counterID, int optionID)
        {
            if (counterID >= _xpCounters.Length || optionID > 30) throw new Exception("Invalid Id given.");
            SetXpCounter(counterID, 0);
            _trackedXpCounters[counterID] = optionID;
            RefreshTrackedXpCounter(counterID);
        }

        /// <summary>
        /// Set's xp counter.
        /// </summary>
        /// <param name="counterID">The counter identifier.</param>
        /// <param name="xp">The xp.</param>
        /// <exception cref="Exception">Invalid counter Id value.</exception>
        public void SetXpCounter(int counterID, double xp)
        {
            if (counterID >= _xpCounters.Length) throw new Exception("Invalid counter Id value.");
            if (_xpCounters[counterID] != xp)
            {
                _xpCounters[counterID] = (int)xp;
                RefreshXpCounter(counterID);
            }
        }

        /// <summary>
        /// Refreshe's hitpoints orb on client.
        /// TODO - update this in the interface, by events.
        /// </summary>
        public void RefreshLifePoints() => _owner.Configurations.SendStandardConfiguration(1240, LifePoints * 2);

        /// <summary>
        /// Set's character hitpoints, if the amount
        /// is 0 or lower the character.OnDeath() get's called.
        /// </summary>
        /// <param name="hp">The hp.</param>
        public void SetLifePoints(int hp)
        {
            if (LifePoints != hp)
            {
                LifePoints = (short)hp;
                RefreshLifePoints();
            }

            if (hp <= 0) _owner.OnDeath();
        }

        /// <summary>
        /// Refreshe's prayer points.
        /// </summary>
        public void RefreshPrayerPoints() => _owner.Configurations.SendStandardConfiguration(2382, PrayerPoints);

        /// <summary>
        /// Set's prayer points.
        /// If prayer points are lower or equal to 0 and
        /// character is praying then it sends message called
        /// 'You have run out of Prayer points; you can recharge at an altar.'
        /// and deactivates all prayers.
        /// </summary>
        /// <param name="pp">The pp.</param>
        public void SetPrayerPoints(int pp)
        {
            if (PrayerPoints != pp)
            {
                PrayerPoints = pp;
                RefreshPrayerPoints();
            }

            if (pp <= 0 && _owner.Prayers.Praying)
            {
                _owner.Prayers.QuickPrayer.TurnOffQuickPrayer();
                _owner.Prayers.DeactivateAllPrayers();
                _owner.SendChatMessage("You have run out of Prayer points; you can recharge at an altar.");
            }
        }

        /// <summary>
        /// Refreshe's run energy.
        /// </summary>
        public void RefreshRunEnergy() =>
            _owner.Session.SendMessage(new SetRunEnergyMessage
            {
                Energy = RunEnergy
            });

        /// <summary>
        /// Drain's run energy by specified amount.
        /// </summary>
        /// <param name="amount">Amount to drain.</param>
        /// <returns>Amount that was drained.</returns>
        public int DrainRunEnergy(int amount)
        {
            if (amount > RunEnergy) amount = RunEnergy;
            SetRunEnergy(RunEnergy - amount);
            return amount;
        }

        /// <summary>
        /// Heal's run energy by specified amount.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Amount that was healed.</returns>
        public int HealRunEnergy(int amount)
        {
            if (RunEnergy + amount >= 100) amount = 100 - RunEnergy;
            SetRunEnergy(RunEnergy + amount);
            return amount;
        }

        /// <summary>
        /// Set's run energy.
        /// </summary>
        /// <param name="re">Amount of energy.</param>
        public void SetRunEnergy(int re)
        {
            if (RunEnergy == re)
            {
                return;
            }

            RunEnergy = re;
            RefreshRunEnergy();
            if (RunEnergy <= 0)
            {
                _owner.Mediator.Publish(new ProfileSetBoolAction(ProfileConstants.RunSettingsToggled, false));
            }
        }

        /// <summary>
        /// Refreshe's skills.
        /// </summary>
        public void RefreshSkills()
        {
            for (var skill = 0; skill < SkillsCount; skill++) RefreshSkill(skill);
        }

        /// <summary>
        /// Refreshe's given skill.
        /// </summary>
        /// <param name="skillID">Id of the skill to refresh.</param>
        public void RefreshSkill(int skillID) =>
            _owner.Session.SendMessage(new SetSkillMessage
            {
                Id = skillID, Level = _skillData[skillID].Level, Experience = (int)_skillData[skillID].Experience
            });

        /// <summary>
        /// Refreshes the skill targets.
        /// </summary>
        public void RefreshSkillTargets()
        {
            var targetHash = 0;
            var targetLevelHash = 0;
            for (var i = 1; i < SkillsCount + 1; i++)
            {
                var shiftValue = ClientSkillIDs[i - 1] + 1;
                if (_skillData[i - 1].TargetExperience != -1)
                {
                    targetHash += 1 << shiftValue;
                    _owner.Configurations.SendStandardConfiguration(1969 + ClientSkillIDs[i - 1], (int)_skillData[i - 1].TargetExperience);
                }
                else if (_skillData[i - 1].TargetLevel != -1)
                {
                    targetHash += 1 << shiftValue;
                    targetLevelHash += 1 << shiftValue;
                    _owner.Configurations.SendStandardConfiguration(1969 + ClientSkillIDs[i - 1], _skillData[i - 1].TargetLevel);
                }
            }

            _owner.Configurations.SendStandardConfiguration(1966, targetHash);
            _owner.Configurations.SendStandardConfiguration(1968, targetLevelHash);
        }

        /// <summary>
        /// Refreshes the skill target.
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        public void RefreshSkillTarget(int skillID)
        {
            var targetHash = 0;
            var targetLevelHash = 0;
            for (var i = 1; i < SkillsCount + 1; i++)
            {
                var shiftValue = ClientSkillIDs[i - 1] + 1;
                if (_skillData[i - 1].TargetExperience != -1)
                    targetHash += 1 << shiftValue;
                else if (_skillData[i - 1].TargetLevel != -1)
                {
                    targetHash += 1 << shiftValue;
                    targetLevelHash += 1 << shiftValue;
                }
            }

            _owner.Configurations.SendStandardConfiguration(1966, targetHash);
            _owner.Configurations.SendStandardConfiguration(1968, targetLevelHash);
            if (_skillData[skillID].TargetLevel != -1)
                _owner.Configurations.SendStandardConfiguration(1969 + ClientSkillIDs[skillID], _skillData[skillID].TargetLevel);
            else if (_skillData[skillID].TargetExperience != -1)
                _owner.Configurations.SendStandardConfiguration(1969 + ClientSkillIDs[skillID], (int)_skillData[skillID].TargetExperience);
        }

        /// <summary>
        /// Drain's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be drained.</param>
        /// <returns>Return's the actual amount of energy drained.</returns>
        public int DrainSpecialEnergy(int amount)
        {
            if (amount > SpecialEnergy) amount = SpecialEnergy;
            SetSpecialEnergy(SpecialEnergy - amount);
            return amount;
        }

        /// <summary>
        /// Heal's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be healed.</param>
        /// <returns>Return's the actual amount of energy healed.</returns>
        public int HealSpecialEnergy(int amount) => HealSpecialEnergy(amount, MaximumSpecialEnergy);

        /// <summary>
        /// Heal's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be healed.</param>
        /// <param name="max">Maximum amount of special energy character can have.</param>
        /// <returns>Return's the actual amount of energy healed.</returns>
        public int HealSpecialEnergy(int amount, int max)
        {
            if (SpecialEnergy + amount > max) amount = max - SpecialEnergy;
            if (amount < 0) amount = 0;
            SetSpecialEnergy(SpecialEnergy + amount);
            return amount;
        }

        /// <summary>
        /// Drain's character's prayer points.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <returns>Return's the actual amount of damage.</returns>
        public int DrainPrayerPoints(int amount)
        {
            if (amount > PrayerPoints) amount = PrayerPoints;
            SetPrayerPoints(PrayerPoints - amount);
            return amount;
        }


        /// <summary>
        /// Heal's character prayer points by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal prayerpoints.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        public int HealPrayerPoints(int amount) => HealPrayerPoints(amount, GetMaximumPrayerPoints());

        /// <summary>
        /// Heal's character prayer points by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal prayerpoints.</param>
        /// <param name="max">Maximum amount of prayer points character can have.</param>
        /// <returns>Returns the amount of points healed actually.</returns>
        public int HealPrayerPoints(int amount, int max)
        {
            if (PrayerPoints + amount > max) amount = max - PrayerPoints;
            if (amount < 0) amount = 0;
            SetPrayerPoints(PrayerPoints + amount);
            return amount;
        }

        /// <summary>
        /// Damage's character hitpoints.
        /// character.OnDeath() is possible.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <returns>
        /// Return's the actual amount of damage.
        /// </returns>
        public int DamageLifePoints(int amount) => DamageLifePoints(amount, true);

        /// <summary>
        /// Damage's character hitpoints.
        /// character.OnDeath() is possible.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <param name="renderHitBar">if set to <c>true</c> [render hit bar].</param>
        /// <returns>
        /// Return's the actual amount of damage.
        /// </returns>
        public int DamageLifePoints(int amount, bool renderHitBar)
        {
            if (amount > LifePoints) amount = LifePoints;
            var last = LifePoints;
            SetLifePoints(LifePoints - amount);
            if (renderHitBar)
            {
                _owner.QueueHitBar(new HitBar(HitBarType.Regular, last, LifePoints, 0, 0));
            }

            return amount;
        }

        /// <summary>
        /// Heal's character hitpoints by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal hitpoints.</param>
        /// <returns>Returns the amount of points healed.</returns>
        public int HealLifePoints(int amount) => HealLifePoints(amount, GetMaximumLifePoints());

        /// <summary>
        /// Heal's character hitpoints by the given amount.
        /// </summary>
        /// <param name="amount">Amount to heal hitpoints.</param>
        /// <param name="max">Maximum amount of constitution points character can have.</param>
        /// <returns>Returns the amount of points healed.</returns>
        public int HealLifePoints(int amount, int max)
        {
            if (LifePoints + amount > max) amount = max - LifePoints;
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
            if (damage > _skillData[skillID].Level) damage = _skillData[skillID].Level;
            _previousSkillData[skillID].Level = _skillData[skillID].Level;
            _skillData[skillID].Level -= damage;
            if (_previousSkillData[skillID].Level != _skillData[skillID].Level)
            {
                RefreshSkill(skillID);
                if (skillID == Summoning && _skillData[skillID].Level <= 0)
                    _owner.SendChatMessage("You have run out of Summoning points; you can recharge at an obelisk.");
            }

            return damage;
        }

        /// <summary>
        /// Heals (Increases) specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Returns the actual heal amount.</returns>
        public int HealSkill(int skillID, int amount) => HealSkill(skillID, LevelForExperience(skillID), amount);

        /// <summary>
        /// Heals (Increases) specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="max">Maximum level of the skill.</param>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Returns the actual heal amount.</returns>
        public int HealSkill(int skillID, int max, int amount)
        {
            var v = _skillData[skillID].Level + amount;
            if (v > max)
            {
                var amt = max - _skillData[skillID].Level;
                if (amt < 0)
                    amount = 0;
                else
                    amount = amt;
            }

            _previousSkillData[skillID].Level = _skillData[skillID].Level;
            _skillData[skillID].Level += amount;
            if (_previousSkillData[skillID].Level != _skillData[skillID].Level)
            {
                RefreshSkill(skillID);
            }

            return amount;
        }

        /// <summary>
        /// Reset's all skill levels , hitpoints, prayerpoints
        /// to their full values.
        /// </summary>
        public void Normalise()
        {
            for (var skill = 0; skill < SkillsCount; skill++)
            {
                _previousSkillData[skill].Level = _skillData[skill].Level;
                _skillData[skill].Level = LevelForExperience(skill);
                RefreshSkill(skill);
            }

            SetLifePoints(GetMaximumLifePoints());
            SetPrayerPoints(GetMaximumPrayerPoints());
            SetRunEnergy(MaximumRunEnergy);
            SetSpecialEnergy(MaximumSpecialEnergy);
            SetPoisonAmount(0);
            ResetCursePrayerBonuses();
            ResetInstantCursePrayerBonuses();
        }

        /// <summary>
        /// Normalises the skills.
        /// </summary>
        /// <param name="skillIDs">The skills.</param>
        public void NormaliseSkills(int[] skillIDs)
        {
            foreach (var skillID in skillIDs)
            {
                _previousSkillData[skillID].Level = _skillData[skillID].Level;
                _skillData[skillID].Level = LevelForExperience(skillID);
                RefreshSkill(skillID);
            }
        }

        /// <summary>
        /// Normalises the boosted statistics.
        /// </summary>
        public void NormalizeBoostedStatistics()
        {
            for (var skill = 0; skill < SkillsCount; skill++)
            {
                if (skill != Summoning)
                {
                    var max = LevelForExperience(skill);
                    if (_skillData[skill].Level > max)
                    {
                        _previousSkillData[skill].Level = _skillData[skill].Level;
                        _skillData[skill].Level = max;
                        RefreshSkill(skill);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate's character bonuses.
        /// </summary>
        public void CalculateBonuses()
        {
            Bonuses.Reset();
            foreach (var item in _owner.Equipment.OfType<IItem>())
            {
                Bonuses.Add(item.EquipmentDefinition.Bonuses);
            }
        }

        /// <summary>
        /// Add's skill experience.
        /// Experience is multiplied with the xp rate multiplier.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <param name="experience">Amount of experience to add.</param>
        /// <returns>If the skill level was changed.</returns>
        /// <exception cref="Exception"></exception>
        public bool AddExperience(int skillID, double experience)
        {
            if (skillID >= SkillsCount) throw new Exception("Wrong skillID.");
            var combatSkill = skillID <= StatisticsConstants.Magic || skillID == Summoning;
            if (combatSkill)
                experience *= _combatOptions.Value.ExpRate;
            else
                experience *= _skillOptions.Value.ExpRate;

            for (var counterID = 0; counterID < _xpCounters.Length; counterID++)
            {
                if (!_enabledXpCounters[counterID]) continue;
                var trackedSkillID = _trackedXpCounters[counterID];
                if (trackedSkillID == 30 || combatSkill && trackedSkillID == 29 || ClientSkillIDs[skillID] == trackedSkillID)
                {
                    var xpCounter = (ulong)_xpCounters[counterID] + (ulong)experience;
                    if (xpCounter > int.MaxValue) xpCounter = int.MaxValue;
                    SetXpCounter(counterID, xpCounter);
                }
            }

            var previousLevel = LevelForExperience(skillID);
            var toSet = GetSkillExperience(skillID) + experience;
            if (toSet > MaximumExperience) toSet = MaximumExperience;
            SetSkillExperience(skillID, toSet);

            var currentLevel = LevelForExperience(skillID);
            if (currentLevel > previousLevel)
            {
                if (GetSkillLevel(skillID) < currentLevel) HealSkill(skillID, currentLevel - previousLevel);
                _owner.EventManager.SendEvent(new SkillLevelUpEvent(_owner, skillID, previousLevel, currentLevel));
            }

            return previousLevel != currentLevel;
        }

        /// <summary>
        /// Refreshe's flashed skills.
        /// </summary>
        public void RefreshFlashedSkills() => _owner.Configurations.SendStandardConfiguration(1179, SkillsFlashFlag);

        /// <summary>
        /// Set's character poison amount.
        /// </summary>
        /// <param name="amount">The amount.</param>
        public void SetPoisonAmount(int amount)
        {
            if (!Poisoned && amount >= 10) _owner.SendChatMessage(GameStrings.Poisoned);
            if (PoisonAmount != amount)
            {
                PoisonAmount = amount;
                RefreshPoison();
            }
        }

        /// <summary>
        /// Refreshe's poison.
        /// </summary>
        public void RefreshPoison() => _owner.Configurations.SendStandardConfiguration(102, Poisoned ? 1 : 0);

        /// <summary>
        /// Flashe's specific skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <exception cref="Exception"></exception>
        public void FlashSkill(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Wrong skillID.");
            SkillsFlashFlag |= SkillFlashFlags[skillID];
            RefreshFlashedSkills();
        }

        /// <summary>
        /// Stop's flashing specified skill.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <exception cref="Exception"></exception>
        public void StopFlashingSkill(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Wrong skillID.");
            SkillsFlashFlag &= ~SkillFlashFlags[skillID];
            RefreshFlashedSkills();
        }

        /// <summary>
        /// Get's if specific skill is being flashed.
        /// </summary>
        /// <param name="skillID">Id of the skill.</param>
        /// <returns>If the skill is being flashed.</returns>
        /// <exception cref="Exception"></exception>
        public bool SkillFlashed(int skillID)
        {
            if (skillID >= SkillsCount) throw new Exception("Wrong skillID.");
            return (SkillsFlashFlag & SkillFlashFlags[skillID]) != 0;
        }

        /// <summary>
        /// Get's character maximum hitpoints.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetMaximumLifePoints()
        {
            if (_owner.Appearance.IsTransformedToNpc()) return _owner.Appearance.PnpcScript.GetMaximumHitpoints();
            var hpLevel = GetSkillLevel(Constitution);
            var baseHitpoints = hpLevel * 10;
            return baseHitpoints;
        }

        /// <summary>
        /// Get's character maximum prayer points.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetMaximumPrayerPoints()
        {
            var prayerLevel = GetSkillLevel(Prayer);
            var basePrayerPoints = prayerLevel * 10;
            return basePrayerPoints;
        }

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
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
            return true;
        }

        /// <summary>
        /// Decrease's dynamic prayer bonus of specific type by 1.
        /// Does not work if current bonus is lower or equal to maximum.
        /// Type must be curse bonus!
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="max">The max.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool DecreaseCursePrayerBonus(BonusPrayerType type, int max)
        {
            if (PrayerBonuses.GetBonus(type) <= max) return false;
            PrayerBonuses.SetBonus(type, PrayerBonuses.GetBonus(type) - 1);
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
            return true;
        }

        /// <summary>
        /// Reset's all turmoil bonuses.
        /// </summary>
        public void ResetTurmoilBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilDefence, 0);
            _owner.Prayers.RefreshDynamicBonuses();
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
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Reset's all instant prayer bonuses.
        /// </summary>
        internal void ResetInstantCursePrayerBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantDefence, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantRanged, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseInstantMagic, 0);
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Set's turmoil bonuses according to target's skills.
        /// </summary>
        /// <param name="target">The target.</param>
        public void SetTurmoilBonuses(ICreature target)
        {
            var attackBonusLevels = (int)(target.Combat.GetAttackLevel() * 0.15);
            var defenceBonusLevels = (int)(target.Combat.GetDefenceLevel() * 0.15);
            var strengthBonusLevels = (int)(target.Combat.GetStrengthLevel() * 0.10);

            var percentAttack = (int)(attackBonusLevels / (double)GetSkillLevel(Attack) * 100.0);
            var percentDefence = (int)(defenceBonusLevels / (double)GetSkillLevel(Defence) * 100.0);
            var percentStrength = (int)(strengthBonusLevels / (double)GetSkillLevel(Strength) * 100.0);

            if (percentAttack > 15) percentAttack = 15;
            if (percentDefence > 15) percentDefence = 15;
            if (percentStrength > 15) percentStrength = 15;

            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilAttack, percentAttack);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilStrength, percentStrength);
            PrayerBonuses.SetBonus(BonusPrayerType.TurmoilDefence, percentDefence);
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Reset's all dynamic prayer bonuses.
        /// Does not send the 'Your skill_name is not affected by sap/leech curses' message.
        /// </summary>
        internal void ResetCursePrayerBonuses()
        {
            PrayerBonuses.SetBonus(BonusPrayerType.CurseAttack, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseStrength, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseDefence, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseRanged, 0);
            PrayerBonuses.SetBonus(BonusPrayerType.CurseMagic, 0);
            _owner.Prayers.RefreshDynamicBonuses();
            _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
        }

        /// <summary>
        /// Gets the hit points restore value.
        /// </summary>
        /// <returns></returns>
        public int GetLifePointsRestoreValue()
        {
            byte bonus = 0;
            if (_owner.HasState<RestingState>()) bonus += 1;
            if (_owner.HasState<RapidHealState>()) bonus += 1;
            if (_owner.HasState<ListeningToMusicianState>()) bonus += 2;
            if (_owner.HasState<RapidRenewalState>()) bonus += 5;
            return 1 * (1 + bonus);
        }

        /// <summary>
        /// Get's character statistics restore rate.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetStatisticsRestoreRate()
        {
            if (_owner.HasState<RapidRestoreState>())
            {
                return 50;
            }

            return 100;
        }

        /// <summary>
        /// Gets the character run energy restore rate.
        /// The current formula is based on 1 Agility.
        /// </summary>
        /// <returns></returns>
        public int GetRunEnergyRestoreRate()
        {
            if (_owner.Movement.Moving && _owner.Movement.MovementType == MovementType.Run) return 0; // No restoration.
            if (_owner.HasState<RestingState>()) return 350;
            if (_owner.HasState<ListeningToMusicianState>()) return 250;
            return 2260 - GetSkillLevel(Agility) * 10; // 1 energy recovers every 2.25 sec with 1 Agility
        }

        /// <summary>
        /// Tick's character statistics.
        /// </summary>
        public void Tick()
        {
            // hitpoints restore tick
            if (_lifePointsRestoreTick++ > StatisticsHelpers.GetLifePointsRestoreRate())
            {
                _lifePointsRestoreTick = 0;
                if (LifePoints < GetMaximumLifePoints()) HealLifePoints(GetLifePointsRestoreValue());
            }

            // hitpoints normalize tick
            if (_lifePointsNormalizeTick++ > StatisticsHelpers.GetLifePointsNormalizeRate())
            {
                _lifePointsNormalizeTick = 0;
                if (LifePoints > GetMaximumLifePoints()) DamageLifePoints(1, false);
            }

            // standart statistics.
            if (_statisticsRestoreTick++ > GetStatisticsRestoreRate())
            {
                _statisticsRestoreTick = 0;
                for (var i = 0; i < SkillsCount; i++)
                    if (i != Summoning)
                    {
                        var max = LevelForExperience(i);
                        if (_skillData[i].Level < max) HealSkill(i, 1);
                    }
            }

            if (_statisticsNormalizeTick++ > StatisticsHelpers.GetStatisticsNormalizeRate())
            {
                _statisticsNormalizeTick = 0;
                for (var i = 0; i < SkillsCount; i++)
                    if (i != Summoning)
                    {
                        var max = LevelForExperience(i);
                        if (_skillData[i].Level > max) DamageSkill(i, 1);
                    }
            }

            // special energy tick
            if (_specialEnergyRestoreTick++ > StatisticsHelpers.GetSpecialEnergyRestoreRate())
            {
                _specialEnergyRestoreTick = 0;
                HealSpecialEnergy(100);
            }

            // poison tick
            if (_poisonTick++ > StatisticsHelpers.GetPoisonRate() && !_owner.Combat.IsDead)
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
                    if (!Poisoned) _owner.SendChatMessage(GameStrings.PoisonWoreOff);
                }
                else
                    SetPoisonAmount(0);
            }

            // prayer bonus tick
            if (_prayerBonusesTick++ > 50)
            {
                _prayerBonusesTick = 0;
                for (var i = 5; i < 10; i++)
                {
                    var bonus = PrayerBonuses.GetBonus((BonusPrayerType)i);
                    var wasAffected = bonus != 0;
                    if (bonus < 0)
                    {
                        IncreaseCursePrayerBonus((BonusPrayerType)i, 0);
                        bonus++;
                    }
                    else if (bonus > 0)
                    {
                        DecreaseCursePrayerBonus((BonusPrayerType)i, 0);
                        bonus--;
                    }

                    if (wasAffected && bonus == 0)
                        _owner.SendChatMessage(
                            "Your " + CreatureHelperTwo.GetCurseSkillName((BonusPrayerType)i) + " is now unaffected by sap and leech curses.");
                }
            }

            var energyRestoreRate = GetRunEnergyRestoreRate();
            if (energyRestoreRate > 0)
            {
                var energyToRestore = _runEnergyRestoreStatus / energyRestoreRate;
                if (energyToRestore > 0 && energyToRestore <= 100)
                {
                    HealRunEnergy(energyToRestore);
                    _runEnergyRestoreStatus -= energyToRestore * energyRestoreRate;
                }

                _runEnergyRestoreStatus += 600; // 600 ms passed
            }

            if (_owner.Movement.Moving && _owner.Movement.MovementType == MovementType.Run)
            {
                var energyToDrain = _runEnergyDrainStatus / StatisticsHelpers.GetRunEnergyDrainRate();
                if (energyToDrain > 0)
                {
                    DrainRunEnergy(energyToDrain);
                    _runEnergyDrainStatus -= energyToDrain * StatisticsHelpers.GetRunEnergyDrainRate();
                }

                _runEnergyDrainStatus += 600; // 600 ms passed
            }
        }

        public void Hydrate(HydratedStatisticsDto hydration)
        {
            /*
                _xpCounters = StringUtilities.DecodeValues((string)row[57], double.Parse);
                _trackedXpCounters = StringUtilities.DecodeValues((string)row[58], int.Parse);
                _enabledXpCounters = StringUtilities.DecodeValues((string)row[59]);
             */

            LifePoints = hydration.LifePoints;
            PrayerPoints = hydration.PrayerPoints;
            RunEnergy = hydration.RunEnergy;
            SpecialEnergy = hydration.SpecialEnergy;
            PoisonAmount = hydration.PoisonAmount;
            PlayTime = TimeSpan.FromMilliseconds(hydration.PlayTime);

            _xpCounters = hydration.XpCounters;
            _trackedXpCounters = hydration.TrackedXpCounters;
            _enabledXpCounters = hydration.EnabledXpCounters;

            _skillData[Agility].Level = hydration.AgilityLevel;
            _skillData[Agility].Experience = hydration.AgilityExp;
            _skillData[Agility].TargetLevel = hydration.TargetSkillLevels[Agility];
            _skillData[Agility].TargetExperience = hydration.TargetSkillExperiences[Agility];

            _skillData[Attack].Level = hydration.AttackLevel;
            _skillData[Attack].Experience = hydration.AttackExp;
            _skillData[Attack].TargetLevel = hydration.TargetSkillLevels[Attack];
            _skillData[Attack].TargetExperience = hydration.TargetSkillExperiences[Attack];

            _skillData[Defence].Level = hydration.DefenceLevel;
            _skillData[Defence].Experience = hydration.DefenceExp;
            _skillData[Defence].TargetLevel = hydration.TargetSkillLevels[Defence];
            _skillData[Defence].TargetExperience = hydration.TargetSkillExperiences[Defence];

            _skillData[Strength].Level = hydration.StrengthLevel;
            _skillData[Strength].Experience = hydration.StrengthExp;
            _skillData[Strength].TargetLevel = hydration.TargetSkillLevels[Strength];
            _skillData[Strength].TargetExperience = hydration.TargetSkillExperiences[Strength];

            _skillData[Constitution].Level = hydration.ConstitutionLevel;
            _skillData[Constitution].Experience = hydration.ConstitutionExp;
            _skillData[Constitution].TargetLevel = hydration.TargetSkillLevels[Constitution];
            _skillData[Constitution].TargetExperience = hydration.TargetSkillExperiences[Constitution];

            _skillData[Ranged].Level = hydration.RangeLevel;
            _skillData[Ranged].Experience = hydration.RangeExp;
            _skillData[Ranged].TargetLevel = hydration.TargetSkillLevels[Ranged];
            _skillData[Ranged].TargetExperience = hydration.TargetSkillExperiences[Ranged];

            _skillData[Prayer].Level = hydration.PrayerLevel;
            _skillData[Prayer].Experience = hydration.PrayerExp;
            _skillData[Prayer].TargetLevel = hydration.TargetSkillLevels[Prayer];
            _skillData[Prayer].TargetExperience = hydration.TargetSkillExperiences[Prayer];

            _skillData[StatisticsConstants.Magic].Level = hydration.MagicLevel;
            _skillData[StatisticsConstants.Magic].Experience = hydration.MagicExp;
            _skillData[StatisticsConstants.Magic].TargetLevel = hydration.TargetSkillLevels[StatisticsConstants.Magic];
            _skillData[StatisticsConstants.Magic].TargetExperience = hydration.TargetSkillExperiences[StatisticsConstants.Magic];

            _skillData[Cooking].Level = hydration.CookingLevel;
            _skillData[Cooking].Experience = hydration.CookingExp;
            _skillData[Cooking].TargetLevel = hydration.TargetSkillLevels[Cooking];
            _skillData[Cooking].TargetExperience = hydration.TargetSkillExperiences[Cooking];

            _skillData[Woodcutting].Level = hydration.WoodcuttingLevel;
            _skillData[Woodcutting].Experience = hydration.WoodcuttingExp;
            _skillData[Woodcutting].TargetLevel = hydration.TargetSkillLevels[Woodcutting];
            _skillData[Woodcutting].TargetExperience = hydration.TargetSkillExperiences[Woodcutting];

            _skillData[Fletching].Level = hydration.FletchingLevel;
            _skillData[Fletching].Experience = hydration.FletchingExp;
            _skillData[Fletching].TargetLevel = hydration.TargetSkillLevels[Fletching];
            _skillData[Fletching].TargetExperience = hydration.TargetSkillExperiences[Fletching];

            _skillData[Fishing].Level = hydration.FishingLevel;
            _skillData[Fishing].Experience = hydration.FishingExp;
            _skillData[Fishing].TargetLevel = hydration.TargetSkillLevels[Fishing];
            _skillData[Fishing].TargetExperience = hydration.TargetSkillExperiences[Fishing];

            _skillData[Firemaking].Level = hydration.FiremakingLevel;
            _skillData[Firemaking].Experience = hydration.FiremakingExp;
            _skillData[Firemaking].TargetLevel = hydration.TargetSkillLevels[Firemaking];
            _skillData[Firemaking].TargetExperience = hydration.TargetSkillExperiences[Firemaking];

            _skillData[Crafting].Level = hydration.CraftingLevel;
            _skillData[Crafting].Experience = hydration.CraftingExp;
            _skillData[Crafting].TargetLevel = hydration.TargetSkillLevels[Crafting];
            _skillData[Crafting].TargetExperience = hydration.TargetSkillExperiences[Crafting];

            _skillData[Smithing].Level = hydration.SmithingLevel;
            _skillData[Smithing].Experience = hydration.SmithingExp;
            _skillData[Smithing].TargetLevel = hydration.TargetSkillLevels[Smithing];
            _skillData[Smithing].TargetExperience = hydration.TargetSkillExperiences[Smithing];

            _skillData[Mining].Level = hydration.MiningLevel;
            _skillData[Mining].Experience = hydration.MiningExp;
            _skillData[Mining].TargetLevel = hydration.TargetSkillLevels[Mining];
            _skillData[Mining].TargetExperience = hydration.TargetSkillExperiences[Mining];

            _skillData[Herblore].Level = hydration.HerbloreLevel;
            _skillData[Herblore].Experience = hydration.HerbloreExp;
            _skillData[Herblore].TargetLevel = hydration.TargetSkillLevels[Herblore];
            _skillData[Herblore].TargetExperience = hydration.TargetSkillExperiences[Herblore];

            _skillData[Thieving].Level = hydration.ThievingLevel;
            _skillData[Thieving].Experience = hydration.ThievingExp;
            _skillData[Thieving].TargetLevel = hydration.TargetSkillLevels[Thieving];
            _skillData[Thieving].TargetExperience = hydration.TargetSkillExperiences[Thieving];

            _skillData[StatisticsConstants.Slayer].Level = hydration.SlayerLevel;
            _skillData[StatisticsConstants.Slayer].Experience = hydration.SlayerExp;
            _skillData[StatisticsConstants.Slayer].TargetLevel = hydration.TargetSkillLevels[StatisticsConstants.Slayer];
            _skillData[StatisticsConstants.Slayer].TargetExperience = hydration.TargetSkillExperiences[StatisticsConstants.Slayer];

            _skillData[StatisticsConstants.Farming].Level = hydration.FarmingLevel;
            _skillData[StatisticsConstants.Farming].Experience = hydration.FarmingExp;
            _skillData[StatisticsConstants.Farming].TargetLevel = hydration.TargetSkillLevels[StatisticsConstants.Farming];
            _skillData[StatisticsConstants.Farming].TargetExperience = hydration.TargetSkillExperiences[StatisticsConstants.Farming];

            _skillData[Runecrafting].Level = hydration.RunecraftingLevel;
            _skillData[Runecrafting].Experience = hydration.RunecraftingExp;
            _skillData[Runecrafting].TargetLevel = hydration.TargetSkillLevels[Runecrafting];
            _skillData[Runecrafting].TargetExperience = hydration.TargetSkillExperiences[Runecrafting];

            _skillData[Construction].Level = hydration.ConstructionLevel;
            _skillData[Construction].Experience = hydration.ConstructionExp;
            _skillData[Construction].TargetLevel = hydration.TargetSkillLevels[Construction];
            _skillData[Construction].TargetExperience = hydration.TargetSkillExperiences[Construction];

            _skillData[Hunter].Level = hydration.HunterLevel;
            _skillData[Hunter].Experience = hydration.HunterExp;
            _skillData[Hunter].TargetLevel = hydration.TargetSkillLevels[Hunter];
            _skillData[Hunter].TargetExperience = hydration.TargetSkillExperiences[Hunter];

            _skillData[Summoning].Level = hydration.SummoningLevel;
            _skillData[Summoning].Experience = hydration.SummoningExp;
            _skillData[Summoning].TargetLevel = hydration.TargetSkillLevels[Summoning];
            _skillData[Summoning].TargetExperience = hydration.TargetSkillExperiences[Summoning];

            _skillData[Dungeoneering].Level = hydration.DungeoneeringLevel;
            _skillData[Dungeoneering].Experience = hydration.DungeoneeringExp;
            _skillData[Dungeoneering].TargetLevel = hydration.TargetSkillLevels[Dungeoneering];
            _skillData[Dungeoneering].TargetExperience = hydration.TargetSkillExperiences[Dungeoneering];
        }

        public HydratedStatisticsDto Dehydrate()
        {
            var hydration = new HydratedStatisticsDto
            {
                AttackLevel = _skillData[Attack].Level,
                AttackExp = _skillData[Attack].Experience,
                DefenceLevel = _skillData[Defence].Level,
                DefenceExp = _skillData[Defence].Experience,
                StrengthLevel = _skillData[Strength].Level,
                StrengthExp = _skillData[Strength].Experience,
                ConstitutionLevel = _skillData[Constitution].Level,
                ConstitutionExp = _skillData[Constitution].Experience,
                RangeLevel = _skillData[Ranged].Level,
                RangeExp = _skillData[Ranged].Experience,
                PrayerLevel = _skillData[Prayer].Level,
                PrayerExp = _skillData[Prayer].Experience,
                MagicLevel = _skillData[StatisticsConstants.Magic].Level,
                MagicExp = _skillData[StatisticsConstants.Magic].Experience,
                CookingLevel = _skillData[Cooking].Level,
                CookingExp = _skillData[Cooking].Experience,
                WoodcuttingLevel = _skillData[Woodcutting].Level,
                WoodcuttingExp = _skillData[Woodcutting].Experience,
                FletchingLevel = _skillData[Fletching].Level,
                FletchingExp = _skillData[Fletching].Experience,
                FishingLevel = _skillData[Fishing].Level,
                FishingExp = _skillData[Fishing].Experience,
                FiremakingLevel = _skillData[Firemaking].Level,
                FiremakingExp = _skillData[Firemaking].Experience,
                CraftingLevel = _skillData[Crafting].Level,
                CraftingExp = _skillData[Crafting].Experience,
                SmithingLevel = _skillData[Smithing].Level,
                SmithingExp = _skillData[Smithing].Experience,
                MiningLevel = _skillData[Mining].Level,
                MiningExp = _skillData[Mining].Experience,
                HerbloreLevel = _skillData[Herblore].Level,
                HerbloreExp = _skillData[Herblore].Experience,
                AgilityLevel = _skillData[Agility].Level,
                AgilityExp = _skillData[Agility].Experience,
                ThievingLevel = _skillData[Thieving].Level,
                ThievingExp = _skillData[Thieving].Experience,
                SlayerLevel = _skillData[StatisticsConstants.Slayer].Level,
                SlayerExp = _skillData[StatisticsConstants.Slayer].Experience,
                FarmingLevel = _skillData[StatisticsConstants.Farming].Level,
                FarmingExp = _skillData[StatisticsConstants.Farming].Experience,
                RunecraftingLevel = _skillData[Runecrafting].Level,
                RunecraftingExp = _skillData[Runecrafting].Experience,
                ConstructionLevel = _skillData[Construction].Level,
                ConstructionExp = _skillData[Construction].Experience,
                HunterLevel = _skillData[Hunter].Level,
                HunterExp = _skillData[Hunter].Experience,
                SummoningLevel = _skillData[Summoning].Level,
                SummoningExp = _skillData[Summoning].Experience,
                DungeoneeringLevel = _skillData[Dungeoneering].Level,
                DungeoneeringExp = _skillData[Dungeoneering].Experience,
                LifePoints = LifePoints,
                PrayerPoints = PrayerPoints,
                RunEnergy = RunEnergy,
                SpecialEnergy = SpecialEnergy,
                PoisonAmount = PoisonAmount,
                PlayTime = (long)PlayTime.Add(DateTimeOffset.Now - _owner.LastLogin).TotalMilliseconds,
                XpCounters = _xpCounters,
                TrackedXpCounters = _trackedXpCounters,
                EnabledXpCounters = _enabledXpCounters,
                TargetSkillLevels =
                [
                    _skillData[Attack].TargetLevel,
                    _skillData[Defence].TargetLevel,
                    _skillData[Strength].TargetLevel,
                    _skillData[Constitution].TargetLevel,
                    _skillData[Ranged].TargetLevel,
                    _skillData[Prayer].TargetLevel,
                    _skillData[StatisticsConstants.Magic].TargetLevel,
                    _skillData[Cooking].TargetLevel,
                    _skillData[Woodcutting].TargetLevel,
                    _skillData[Fletching].TargetLevel,
                    _skillData[Fishing].TargetLevel,
                    _skillData[Firemaking].TargetLevel,
                    _skillData[Crafting].TargetLevel,
                    _skillData[Smithing].TargetLevel,
                    _skillData[Mining].TargetLevel,
                    _skillData[Herblore].TargetLevel,
                    _skillData[Agility].TargetLevel,
                    _skillData[Thieving].TargetLevel,
                    _skillData[StatisticsConstants.Slayer].TargetLevel,
                    _skillData[StatisticsConstants.Farming].TargetLevel,
                    _skillData[Runecrafting].TargetLevel,
                    _skillData[Construction].TargetLevel,
                    _skillData[Hunter].TargetLevel,
                    _skillData[Summoning].TargetLevel,
                    _skillData[Dungeoneering].TargetLevel
                ],
                TargetSkillExperiences =
                [
                    _skillData[Attack].TargetExperience,
                    _skillData[Defence].TargetExperience,
                    _skillData[Strength].TargetExperience,
                    _skillData[Constitution].TargetExperience,
                    _skillData[Ranged].TargetExperience,
                    _skillData[Prayer].TargetExperience,
                    _skillData[StatisticsConstants.Magic].TargetExperience,
                    _skillData[Cooking].TargetExperience,
                    _skillData[Woodcutting].TargetExperience,
                    _skillData[Fletching].TargetExperience,
                    _skillData[Fishing].TargetExperience,
                    _skillData[Firemaking].TargetExperience,
                    _skillData[Crafting].TargetExperience,
                    _skillData[Smithing].TargetExperience,
                    _skillData[Mining].TargetExperience,
                    _skillData[Herblore].TargetExperience,
                    _skillData[Agility].TargetExperience,
                    _skillData[Thieving].TargetExperience,
                    _skillData[StatisticsConstants.Slayer].TargetExperience,
                    _skillData[StatisticsConstants.Farming].TargetExperience,
                    _skillData[Runecrafting].TargetExperience,
                    _skillData[Construction].TargetExperience,
                    _skillData[Hunter].TargetExperience,
                    _skillData[Summoning].TargetExperience,
                    _skillData[Dungeoneering].TargetExperience
                ]
            };
            return hydration;
        }
    }
}