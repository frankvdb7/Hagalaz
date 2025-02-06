using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use CharactersProfiles instead")]
    public partial class CharactersPreference
    {
        public uint MasterId { get; set; }
        public byte SingleMouse { get; set; }
        public byte ChatEffects { get; set; }
        public byte SplitChat { get; set; }
        public byte AcceptAid { get; set; }
        public byte FilterProfanity { get; set; }
        public byte Running { get; set; }
        public string BankTabs { get; set; } = null!;
        public int Bankx { get; set; }
        public string FcName { get; set; } = null!;
        public string FcLastEntered { get; set; } = null!;
        public sbyte FcRankEnter { get; set; }
        public sbyte FcRankTalk { get; set; }
        public sbyte FcRankKick { get; set; }
        public sbyte FcRankLoot { get; set; }
        public byte FcLootShare { get; set; }
        public string CcLastEntered { get; set; } = null!;
        public string GuestCcLastEntered { get; set; } = null!;
        public byte PmAvailability { get; set; }
        public byte DefensiveCasting { get; set; }
        public ushort MagicBook { get; set; }
        public byte PrayerBook { get; set; }
        public byte AttackStyleOptionId { get; set; }
        public byte AutoRetaliating { get; set; }
        public byte HideCombatSpells { get; set; }
        public byte HideTeleportSpells { get; set; }
        public byte HideMiscSpells { get; set; }
        public byte HideSkillSpells { get; set; }
        public byte SumLeftClickOption { get; set; }
        public byte GameFilter { get; set; }
        public byte FriendsFilter { get; set; }
        public byte ClanFilter { get; set; }
        public byte AssistFilter { get; set; }
        public byte TradeFilter { get; set; }
        public byte PublicFilter { get; set; }
        public string QuickPrayers { get; set; } = null!;
        public byte RightClickReporting { get; set; }
        public byte XpCounterPopup { get; set; }
        public byte XpCounterDisplay { get; set; }
        public byte MoneyPouchDisplay { get; set; }
    }
}
