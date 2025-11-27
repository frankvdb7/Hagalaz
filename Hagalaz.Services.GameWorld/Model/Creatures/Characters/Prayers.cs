using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains character prayers stuff.
    /// </summary>
    public class Prayers : IPrayers
    {

        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contains prayed prayers status.
        /// </summary>
        private readonly int[] _prayerStatus;

        /// <summary>
        /// Contains prayers drain status.
        /// </summary>
        private readonly int[] _prayersDrainStatus;

        private readonly IGraphicBuilder _graphicBuilder;
        private readonly IAnimationBuilder _animationBuilder;

        /// <summary>
        /// Gets the quick prayer.
        /// </summary>
        public IQuickPrayer QuickPrayer { get; }

        /// <summary>
        /// Tell's if character is currently praying.
        /// </summary>
        public bool Praying => _prayerStatus.Any(status => status != 0);

        /// <summary>
        /// Construct's new prayers component.
        /// </summary>
        /// <param name="owner"></param>
        public Prayers(ICharacter owner)
        {
            _owner = owner;
            _prayerStatus = new int[30];
            _prayersDrainStatus = new int[30];
            QuickPrayer = new QuickPrayer(owner);
            _animationBuilder = owner.ServiceProvider.GetRequiredService<IAnimationBuilder>();
            _graphicBuilder = owner.ServiceProvider.GetRequiredService<IGraphicBuilder>();
        }

        /// <summary>
        /// Tick's character's prayers.
        /// </summary>
        public void Tick()
        {
            for (var i = 0; i < _prayerStatus.Length; i++)
            {
                if (_prayerStatus[i] == 0)
                    continue;
                int drainRate;
                if ((_prayerStatus[i] & 0x4000) != 0) // curse
                    drainRate = GetDrainRate((AncientCurses)_prayerStatus[i]);
                else // prayer
                    drainRate = GetDrainRate((NormalPrayer)_prayerStatus[i]);
                var pointsToDrain = _prayersDrainStatus[i] / drainRate;
                if (pointsToDrain > 0)
                {
                    _owner.Statistics.DrainPrayerPoints(pointsToDrain);
                    _prayersDrainStatus[i] -= pointsToDrain * drainRate;
                }

                _prayersDrainStatus[i] += 600; // 600 ms passed
            }
        }

        /// <summary>
        /// Refreshe's client configurations.
        /// </summary>
        public void RefreshConfigurations()
        {
            RefreshDynamicBonuses();
            RefreshPrayerActivation();
        }

        /// <summary>
        /// Refreshe's dynamic bonuses.
        /// </summary>
        public void RefreshDynamicBonuses()
        {
            var bonuses = _owner.Statistics.PrayerBonuses;
            var total = 0;
            total |= bonuses.GetBonus(BonusPrayerType.CurseAttack) + bonuses.GetBonus(BonusPrayerType.TurmoilAttack) + bonuses.GetBonus(BonusPrayerType.CurseInstantAttack) + 30;
            total |= bonuses.GetBonus(BonusPrayerType.CurseStrength) + bonuses.GetBonus(BonusPrayerType.TurmoilStrength) + bonuses.GetBonus(BonusPrayerType.CurseInstantStrength) + 30 << 6;
            total |= bonuses.GetBonus(BonusPrayerType.CurseDefence) + bonuses.GetBonus(BonusPrayerType.TurmoilDefence) + bonuses.GetBonus(BonusPrayerType.CurseInstantDefence) + 30 << 12;
            total |= _owner.Statistics.PrayerBonuses.GetBonus(BonusPrayerType.CurseRanged) + bonuses.GetBonus(BonusPrayerType.CurseInstantRanged) + 30 << 18;
            total |= _owner.Statistics.PrayerBonuses.GetBonus(BonusPrayerType.CurseMagic) + bonuses.GetBonus(BonusPrayerType.CurseInstantMagic) + 30 << 24;
            _owner.Configurations.SendStandardConfiguration(1583, total);
        }

        /// <summary>
        /// Refreshe's static bonuses.
        /// </summary>
        private void RefreshPrayerActivation()
        {
            var value = 0;
            var book = _owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
            switch (book)
            {
                case PrayerBook.StandardBook:
                    {
                        for (var i = 0; i < _prayerStatus.Length; i++)
                            if (_prayerStatus[i] != 0)
                                value |= 1 << PrayerConstants.StandardPrayersActivators[i];
                        _owner.Configurations.SendStandardConfiguration(1395, value);
                        break;
                    }
                case PrayerBook.CursesBook:
                    {
                        for (var i = 0; i < _prayerStatus.Length; i++)
                            if (_prayerStatus[i] != 0)
                                value |= 1 << i;
                        _owner.Configurations.SendStandardConfiguration(1582, value);
                        break;
                    }
            }
        }

        /// <summary>
        /// Get's if character is praying specific prayer.
        /// </summary>
        /// <returns></returns>
        public bool IsPraying(NormalPrayer prayer) => _prayerStatus[(int)prayer & 0xFF] == (int)prayer;

        /// <summary>
        /// Get's if character is praying specific prayer.
        /// </summary>
        /// <returns></returns>
        public bool IsPraying(AncientCurses prayer) => _prayerStatus[(int)prayer & 0xFF] == (int)prayer;

        /// <summary>
        /// Check's requirements for specific prayer.
        /// </summary>
        /// <returns></returns>
        private bool CheckRequirements(NormalPrayer prayer) => _owner.Statistics.LevelForExperience(StatisticsConstants.Prayer) >= PrayerConstants.StandardLevelRequirements[(int)prayer & 0xFF];

        /// <summary>
        /// Check's requirements for specific curse.
        /// </summary>
        /// <returns></returns>
        private bool CheckRequirements(AncientCurses curse) => _owner.Statistics.LevelForExperience(StatisticsConstants.Prayer) >= PrayerConstants.CursesLevelRequirements[(int)curse & 0xFF];

        /// <summary>
        /// Get's drain rate of specific prayer.
        /// </summary>
        /// <returns></returns>
        private int GetDrainRate(NormalPrayer prayer)
        {
            double baseRate = PrayerConstants.StandardDrainAmounts[(int)prayer & 0xFF];
            // adjust the base rate for specific items here ( such as castlewars halo )
            // ------------------------------------------------------------------------
            baseRate *= 1.0 + _owner.Statistics.Bonuses.GetBonus(BonusType.Prayer) / 30.0;
            return (int)baseRate;
        }

        /// <summary>
        /// Get's drain rate of specific curse.
        /// </summary>
        /// <returns></returns>
        private int GetDrainRate(AncientCurses prayer)
        {
            double baseRate = PrayerConstants.CursesDrainAmounts[(int)prayer & 0xFF];
            // adjust the base rate for specific items here ( such as castlewars halo )
            // ------------------------------------------------------------------------
            baseRate *= 1.0 + _owner.Statistics.Bonuses.GetBonus(BonusType.Prayer) / 30.0;
            return (int)baseRate;
        }

        /// <summary>
        /// Activate's specific prayer.
        /// This method does nothing if prayer is already activated or book is not
        /// standart book.
        /// </summary>
        public void ActivatePrayer(NormalPrayer prayer)
        {
            var book = _owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
            if (!_owner.EventManager.SendEvent(new PrayerAllowEvent(_owner, prayer)) || book != PrayerBook.StandardBook || IsPraying(prayer))
                return;
            if (!CheckRequirements(prayer))
            {
                _owner.SendChatMessage("You need prayer level of " + PrayerConstants.StandardLevelRequirements[(int)prayer & 0xFF] + " to use this prayer.");
                return;
            }

            if (_owner.Statistics.PrayerPoints <= 0)
            {
                _owner.SendChatMessage("You need to recharge your Prayer at an altar.");
                return;
            }

            // This uses another method of deactivating prayers. The other implementation has its benefits aswell.
            var deactivates = PrayerConstants.StandardPrayersDeactivators[(int)prayer & 0xFF];
            foreach (var deactivate in deactivates)
            {
                DeactivatePrayer(PrayerConstants.StandardPrayers[deactivate]);
            }

            switch (prayer)
            {
                case NormalPrayer.ThickSkin:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.BurstOfStrength:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ClarityOfThought:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SharpEye:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticRanged, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticWill:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticMagic, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.RockSkin:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SuperhumanStrength:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ImprovedReflexes:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.HawkEye:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticRanged, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticLore:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticMagic, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SteelSkin:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.UltimateStrength:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.IncredibleReflexes:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.EagleEye:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticRanged, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticMight:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticMagic, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Chivalry:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 18);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 20);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Piety:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 20);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 23);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Rigour:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticRanged, 20);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Augury:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticMagic, 20);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ProtectFromSummoning when _owner.Appearance.PrayerIcon == PrayerIcon.None:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Summoning;
                    break;
                case NormalPrayer.ProtectFromSummoning:
                    {
                        var icon = _owner.Appearance.PrayerIcon;
                        if (icon == PrayerIcon.ProtectFromMelee)
                            icon = PrayerIcon.MeleeSummoning;
                        else if (icon == PrayerIcon.ProtectFromRange)
                            icon = PrayerIcon.RangeSummoning;
                        else if (icon == PrayerIcon.ProtectFromMagic)
                            icon = PrayerIcon.MageSummoning;
                        _owner.Appearance.PrayerIcon = icon;
                        break;
                    }
                case NormalPrayer.ProtectFromMagic when _owner.Appearance.PrayerIcon == PrayerIcon.Summoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.MageSummoning;
                    break;
                case NormalPrayer.ProtectFromMagic:
                    _owner.Appearance.PrayerIcon = PrayerIcon.ProtectFromMagic;
                    break;
                case NormalPrayer.ProtectFromRanged when _owner.Appearance.PrayerIcon == PrayerIcon.Summoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.RangeSummoning;
                    break;
                case NormalPrayer.ProtectFromRanged:
                    _owner.Appearance.PrayerIcon = PrayerIcon.ProtectFromRange;
                    break;
                case NormalPrayer.ProtectFromMelee when _owner.Appearance.PrayerIcon == PrayerIcon.Summoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.MeleeSummoning;
                    break;
                case NormalPrayer.ProtectFromMelee:
                    _owner.Appearance.PrayerIcon = PrayerIcon.ProtectFromMelee;
                    break;
                case NormalPrayer.Redemption:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Redemption;
                    _owner.AddState(new RedemptionState());
                    break;
                case NormalPrayer.Retribution:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Retribution;
                    _owner.AddState(new RetributionState());
                    break;
                case NormalPrayer.Smite:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Smite;
                    break;
                case NormalPrayer.RapidRestore:
                    _owner.AddState(new RapidRestoreState());
                    break;
                case NormalPrayer.RapidHeal:
                    _owner.AddState(new RapidHealState());
                    break;
                case NormalPrayer.RapidRenewal:
                    _owner.AddState(new RapidRenewalState());
                    break;
                case NormalPrayer.ProtectItem:
                    _owner.AddState(new ProtectOneItemState());
                    break;
                default:
                    _owner.SendChatMessage("Prayer " + prayer + " is not implemented yet.");
                    break;
            }

            _prayerStatus[(int)prayer & 0xFF] = (int)prayer;
            _prayersDrainStatus[(int)prayer & 0xFF] = 0;
            RefreshPrayerActivation();
        }

        /// <summary>
        /// Activate's specific curse;
        /// This method does nothing if prayer is already activated or book is not
        /// standart book.
        /// </summary>
        public void ActivatePrayer(AncientCurses prayer)
        {
            var book = _owner.Profile.GetValue<PrayerBook>(ProfileConstants.PrayerSettingsBook);
            if (!_owner.EventManager.SendEvent(new PrayerAllowEvent(_owner, prayer)) || book != PrayerBook.CursesBook || IsPraying(prayer))
                return;
            if (!CheckRequirements(prayer))
            {
                _owner.SendChatMessage("You need prayer level of " + PrayerConstants.CursesLevelRequirements[(int)prayer & 0xFF] + " to use this curse.");
                return;
            }

            if (_owner.Statistics.PrayerPoints <= 0)
            {
                _owner.SendChatMessage("You need to recharge your Prayer at an altar.");
                return;
            }

            var deactivates1 = (int)prayer >> 16 & 0xF;
            var deactivates2 = (int)prayer >> 20 & 0xF;
            var deactivates3 = (int)prayer >> 24 & 0xF;
            foreach (var status in _prayerStatus)
            {
                var group = (status & ~0x4000) >> 8 & 0xFF;
                if (deactivates1 != 0 && @group == deactivates1 || deactivates2 != 0 && @group == deactivates2 || deactivates3 != 0 && @group == deactivates3)
                    DeactivatePrayer((AncientCurses)status);
            }

            switch (prayer)
            {
                case AncientCurses.ProtectItem:
                    _owner.AddState(new ProtectOneItemState());
                    _owner.QueueAnimation(_animationBuilder.Create().WithId(12567).Build());
                    _owner.QueueGraphic(_graphicBuilder.Create().WithId(2213).Build());
                    break;
                case AncientCurses.DeflectSummoning when _owner.Appearance.PrayerIcon == PrayerIcon.None:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectSummoning;
                    break;
                case AncientCurses.DeflectSummoning:
                    {
                        var icon = _owner.Appearance.PrayerIcon;
                        if (icon == PrayerIcon.DeflectMelee)
                            icon = PrayerIcon.DeflectMeleeSummoning;
                        else if (icon == PrayerIcon.DeflectRange)
                            icon = PrayerIcon.DeflectRangeSummoning;
                        else if (icon == PrayerIcon.DeflectMage)
                            icon = PrayerIcon.DeflectMageSummoning;
                        _owner.Appearance.PrayerIcon = icon;
                        break;
                    }
                case AncientCurses.DeflectMelee when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectMeleeSummoning;
                    break;
                case AncientCurses.DeflectMelee:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectMelee;
                    break;
                case AncientCurses.DeflectMissiles when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectRangeSummoning;
                    break;
                case AncientCurses.DeflectMissiles:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectRange;
                    break;
                case AncientCurses.DeflectMagic when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectMageSummoning;
                    break;
                case AncientCurses.DeflectMagic:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectMage;
                    break;
                case AncientCurses.SoulSplit:
                    _owner.Appearance.PrayerIcon = PrayerIcon.SoulSplit;
                    break;
                case AncientCurses.Turmoil:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 15);
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 23);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    _owner.AddState(new TurmoilState());
                    _owner.QueueAnimation(_animationBuilder.Create().WithId(12565).Build());
                    _owner.QueueGraphic(_graphicBuilder.Create().WithId(2256).Build());
                    break;
                case AncientCurses.Berserker:
                    _owner.AddState(new BerserkerState());
                    _owner.QueueAnimation(_animationBuilder.Create().WithId(12589).Build());
                    _owner.QueueGraphic(_graphicBuilder.Create().WithId(2266).Build());
                    break;
                case AncientCurses.SapWarrior:
                    _owner.AddState(new SapWarriorState());
                    break;
                case AncientCurses.SapRanger:
                    _owner.AddState(new SapRangerState());
                    break;
                case AncientCurses.SapMage:
                    _owner.AddState(new SapMagerState());
                    break;
                case AncientCurses.SapSpirit:
                    _owner.AddState(new SapSpiritState());
                    break;
                case AncientCurses.LeechAttack:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticAttack, 5);
                    _owner.AddState(new LeechAttackState());
                    break;
                case AncientCurses.LeechStrength:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticStrength, 5);
                    _owner.AddState(new LeechStrengthState());
                    break;
                case AncientCurses.LeechDefence:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticDefence, 5);
                    _owner.AddState(new LeechDefenceState());
                    break;
                case AncientCurses.LeechRanged:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticRanged, 5);
                    _owner.AddState(new LeechRangedState());
                    break;
                case AncientCurses.LeechMagic:
                    _owner.Statistics.PrayerBonuses.AddToBonus(BonusPrayerType.StaticMagic, 5);
                    _owner.AddState(new LeechMagicState());
                    break;
                case AncientCurses.LeechEnergy:
                    _owner.AddState(new LeechEnergyState());
                    break;
                case AncientCurses.LeechSpecialAttack:
                    _owner.AddState(new LeechSpecialState());
                    break;
                default:
                    _owner.SendChatMessage("Curse " + prayer + " is not implemented yet.");
                    break;
            }

            _prayerStatus[(int)prayer & 0xFF] = (int)prayer;
            _prayersDrainStatus[(int)prayer & 0xFF] = 0;
            RefreshPrayerActivation();
        }

        /// <summary>
        /// Deactivate's specific prayer.
        /// This method does nothing if prayer is not activated.
        /// </summary>
        /// <param name="prayer"></param>
        public void DeactivatePrayer(NormalPrayer prayer)
        {
            if (!IsPraying(prayer))
                return;
            switch (prayer)
            {
                case NormalPrayer.ThickSkin:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.BurstOfStrength:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ClarityOfThought:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SharpEye:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticRanged, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticWill:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticMagic, 5);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.RockSkin:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SuperhumanStrength:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ImprovedReflexes:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.HawkEye:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticRanged, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticLore:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticMagic, 10);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.SteelSkin:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.UltimateStrength:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.IncredibleReflexes:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.EagleEye:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticRanged, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.MysticMight:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticMagic, 15);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Chivalry:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 18);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 20);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Piety:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 20);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 23);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Rigour:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticRanged, 20);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.Augury:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticMagic, 20);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 25);
                    _owner.EventManager.SendEvent(new CreaturePrayerBonusChangedEvent(_owner));
                    break;
                case NormalPrayer.ProtectFromSummoning when _owner.Appearance.PrayerIcon == PrayerIcon.Summoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case NormalPrayer.ProtectFromSummoning:
                    {
                        var icon = _owner.Appearance.PrayerIcon;
                        if (icon == PrayerIcon.MeleeSummoning)
                            icon = PrayerIcon.ProtectFromMelee;
                        else if (icon == PrayerIcon.RangeSummoning)
                            icon = PrayerIcon.ProtectFromRange;
                        else if (icon == PrayerIcon.MageSummoning)
                            icon = PrayerIcon.ProtectFromMagic;
                        _owner.Appearance.PrayerIcon = icon;
                        break;
                    }
                case NormalPrayer.ProtectFromMagic when _owner.Appearance.PrayerIcon == PrayerIcon.MageSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Summoning;
                    break;
                case NormalPrayer.ProtectFromMagic:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case NormalPrayer.ProtectFromRanged when _owner.Appearance.PrayerIcon == PrayerIcon.RangeSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Summoning;
                    break;
                case NormalPrayer.ProtectFromRanged:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case NormalPrayer.ProtectFromMelee when _owner.Appearance.PrayerIcon == PrayerIcon.MeleeSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.Summoning;
                    break;
                case NormalPrayer.ProtectFromMelee:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case NormalPrayer.Redemption:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    _owner.RemoveState<RedemptionState>();
                    break;
                case NormalPrayer.Retribution:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    _owner.RemoveState<RetributionState>();
                    break;
                case NormalPrayer.Smite:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case NormalPrayer.RapidRestore:
                    _owner.RemoveState<RapidRestoreState>();
                    break;
                case NormalPrayer.RapidHeal:
                    _owner.RemoveState<RapidHealState>();
                    break;
                case NormalPrayer.RapidRenewal:
                    _owner.RemoveState<RapidRenewalState>();
                    break;
                case NormalPrayer.ProtectItem:
                    _owner.RemoveState<ProtectOneItemState>();
                    break;
                default:
                    _owner.SendChatMessage("Prayer " + prayer + " is not implemented yet.");
                    break;
            }

            _prayerStatus[(int)prayer & 0xFF] = 0;
            _prayersDrainStatus[(int)prayer & 0xFF] = 0;
            RefreshPrayerActivation();
        }


        /// <summary>
        /// Deactivate's specific prayer.
        /// This method does nothing if prayer is not activated.
        /// </summary>
        /// <param name="prayer"></param>
        public void DeactivatePrayer(AncientCurses prayer)
        {
            if (!IsPraying(prayer))
                return;
            switch (prayer)
            {
                case AncientCurses.ProtectItem:
                    _owner.RemoveState<ProtectOneItemState>();
                    break;
                case AncientCurses.DeflectSummoning when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case AncientCurses.DeflectSummoning:
                    {
                        var icon = _owner.Appearance.PrayerIcon;
                        if (icon == PrayerIcon.DeflectMeleeSummoning)
                            icon = PrayerIcon.DeflectMelee;
                        else if (icon == PrayerIcon.DeflectRangeSummoning)
                            icon = PrayerIcon.DeflectRange;
                        else if (icon == PrayerIcon.DeflectMageSummoning)
                            icon = PrayerIcon.DeflectMage;
                        _owner.Appearance.PrayerIcon = icon;
                        break;
                    }
                case AncientCurses.DeflectMelee when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectMeleeSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectSummoning;
                    break;
                case AncientCurses.DeflectMelee:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case AncientCurses.DeflectMissiles when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectRangeSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectSummoning;
                    break;
                case AncientCurses.DeflectMissiles:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case AncientCurses.DeflectMagic when _owner.Appearance.PrayerIcon == PrayerIcon.DeflectMageSummoning:
                    _owner.Appearance.PrayerIcon = PrayerIcon.DeflectSummoning;
                    break;
                case AncientCurses.DeflectMagic:
                case AncientCurses.SoulSplit:
                    _owner.Appearance.PrayerIcon = PrayerIcon.None;
                    break;
                case AncientCurses.Turmoil:
                    _owner.RemoveState<TurmoilState>();
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 15);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 15);
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 23);
                    _owner.Statistics.ResetTurmoilBonuses();
                    break;
                case AncientCurses.Berserker:
                    _owner.RemoveState<BerserkerState>();
                    break;
                case AncientCurses.SapWarrior:
                    _owner.RemoveState<SapWarriorState>();
                    break;
                case AncientCurses.SapRanger:
                    _owner.RemoveState<SapRangerState>();
                    break;
                case AncientCurses.SapMage:
                    _owner.RemoveState<SapMagerState>();
                    break;
                case AncientCurses.SapSpirit:
                    _owner.RemoveState<SapSpiritState>();
                    break;
                case AncientCurses.LeechAttack:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticAttack, 5);
                    _owner.RemoveState<LeechAttackState>();
                    break;
                case AncientCurses.LeechStrength:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticStrength, 5);
                    _owner.RemoveState<LeechStrengthState>();
                    break;
                case AncientCurses.LeechDefence:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticDefence, 5);
                    _owner.RemoveState<LeechDefenceState>();
                    break;
                case AncientCurses.LeechRanged:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticRanged, 5);
                    _owner.RemoveState<LeechRangedState>();
                    break;
                case AncientCurses.LeechMagic:
                    _owner.Statistics.PrayerBonuses.RemoveFromBonus(BonusPrayerType.StaticMagic, 5);
                    _owner.RemoveState<LeechMagicState>();
                    break;
                case AncientCurses.LeechEnergy:
                    _owner.RemoveState<LeechEnergyState>();
                    break;
                case AncientCurses.LeechSpecialAttack:
                    _owner.RemoveState<LeechSpecialState>();
                    break;
                default:
                    _owner.SendChatMessage("Curse " + prayer + " is not implemented yet.");
                    break;
            }

            _prayerStatus[(int)prayer & 0xFF] = 0;
            _prayersDrainStatus[(int)prayer & 0xFF] = 0;
            RefreshPrayerActivation();
        }

        /// <summary>
        /// Deactivate's all prayers and curses.
        /// </summary>
        public void DeactivateAllPrayers()
        {
            foreach (var status in _prayerStatus)
                if (status != 0)
                {
                    if ((status & 0x4000) == 0)
                        DeactivatePrayer((NormalPrayer)status);
                    else
                        DeactivatePrayer((AncientCurses)status);
                }
        }
    }
}
