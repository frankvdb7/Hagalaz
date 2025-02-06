using System.Text.Json;

namespace Hagalaz.Configuration
{
    public static class ProfileConstants
    {
        /// <summary>
        /// The default options for profile serialization
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// The bank settings
        /// </summary>
        public const string BankSettings = "bank.settings";
        /// <summary>
        /// The bank settings option x
        /// </summary>
        public const string BankSettingsOptionX = BankSettings + ".option_x";
        /// <summary>
        /// The default bank x option value
        /// </summary>
        public const int BankSettingsOptionXDefault = 25;
        /// <summary>
        /// The bank settings tabs
        /// </summary>
        public const string BankSettingsTab = BankSettings + ".tabs";
        /// <summary>
        /// The default bank tabs value
        /// </summary>
        public static readonly int[] BankSettingsTabDefault = new int[8];
        /// <summary>
        /// The combat settings
        /// </summary>
        public const string CombatSettings = "combat.settings";
        /// <summary>
        /// The combat settings attack style option id
        /// </summary>
        public const string CombatSettingsAttackStyleOptionId = CombatSettings + ".attack_style_option_id";
        /// <summary>
        /// The combat settings auto retaliating
        /// </summary>
        public const string CombatSettingsAutoRetaliate = CombatSettings + ".auto_retaliating";
        /// <summary>
        /// The combat settings special attack
        /// </summary>
        public const string CombatSettingsSpecialAttack = CombatSettings + ".special_attack";
        /// <summary>
        /// The magic combat settings
        /// </summary>
        public const string CombatSettingsMagic = CombatSettings + ".magic";
        /// <summary>
        /// The magic combat settings defensive casting
        /// </summary>
        public const string CombatSettingsMagicDefensiveCasting = CombatSettingsMagic + ".defensive_casting";
        /// <summary>
        /// The magic settings
        /// </summary>
        public const string MagicSettings = "magic.settings";
        /// <summary>
        /// The magic settings book
        /// </summary>
        public const string MagicSettingsBook = MagicSettings + ".book";
        /// <summary>
        /// The magic settings hide combat spells
        /// </summary>
        public const string MagicSettingsHideCombatSpells = MagicSettings + ".hide_combat_spells";
        /// <summary>
        /// The magic settings hide skill spells
        /// </summary>
        public const string MagicSettingsHideSkillSpells = MagicSettings + ".hide_skill_spells";
        /// <summary>
        /// The magic settings hide teleport spells
        /// </summary>
        public const string MagicSettingsHideTeleportSpells = MagicSettings + ".hide_teleport_spells";
        /// <summary>
        /// The magic settings hide misc spells
        /// </summary>
        public const string MagicSettingsHideMiscSpells = MagicSettings + ".hide_misc_spells";
        /// <summary>
        /// The magic settings accept aid toggle
        /// </summary>
        public const string MagicSettingsAcceptAid = MagicSettings + "accept_aid_toggled";
        /// <summary>
        /// The summoning settings
        /// </summary>
        public const string SummoningSettings = "summoning.settings";
        /// <summary>
        /// The summoning settings orb option id
        /// </summary>
        public const string SummoningSettingsOrbOptionId = SummoningSettings + ".orb_option_id";
        /// <summary>
        /// The prayer settings
        /// </summary>
        public const string PrayerSettings = "prayer.settings";
        /// <summary>
        /// The prayer settings book
        /// </summary>
        public const string PrayerSettingsBook = PrayerSettings + ".book";
        /// <summary>
        /// The run settings
        /// </summary>
        public const string RunSettings = "run";
        /// <summary>
        /// The run settings toggled;
        /// </summary>
        public const string RunSettingsToggled = RunSettings + ".toggled";
        /// <summary>
        /// The player vs player statistics
        /// </summary>
        public const string PvpStatistics = "pvp.statistics";
        /// <summary>
        /// The pvp kill count key
        /// </summary>
        public const string PvpKillCount = PvpStatistics + ".kill_count";
        /// <summary>
        /// The pvp death count key
        /// </summary>
        public const string PvpDeathCount = PvpStatistics + ".death_count";
        /// <summary>
        /// The slayer statistics
        /// </summary>
        public const string SlayerStatistics = "slayer.statistics";
        /// <summary>
        /// The slayer reward points key
        /// </summary>
        public const string SlayerRewardPoints = SlayerStatistics + ".points";
        /// <summary>
        /// The money pouch settings key
        /// </summary>
        public const string MoneyPouchSettings = "moneypouch.settings";
        /// <summary>
        /// The money pouch toggled
        /// </summary>
        public const string MoneyPouchSettingsToggled = MoneyPouchSettings + ".toggled";
        /// <summary>
        /// The xp counter settings
        /// </summary>
        public const string XpCounterSettings = "xpcounter.settings";
        /// <summary>
        /// The xp counter toggled
        /// </summary>
        public const string XpCounterSettingsToggled = XpCounterSettings + ".toggled";
        /// <summary>
        /// The xp counter popup toggled
        /// </summary>
        public const string XpCounterSettingsPopupToggled = XpCounterSettings + ".popup-toggled";
        /// <summary>
        /// The chat settings
        /// </summary>
        public const string ChatSettings = "chat.settings";
        /// <summary>
        /// The chat settings split chat
        /// </summary>
        public const string ChatSettingsSplitChat = ChatSettings + ".split-chat-toggled";
        /// <summary>
        /// The chat settings effects toggled
        /// </summary>
        public const string ChatSettingsEffects = ChatSettings + ".effects-toggled";
        /// <summary>
        /// The chat settings profanity toggled
        /// </summary>
        public const string ChatSettingsProfanity = ChatSettings + ".profanity-toggled";
        /// <summary>
        /// The chat settings friends filter
        /// </summary>
        public const string ChatSettingsFriendsFilter = ChatSettings + ".friends_filter";
        /// <summary>
        /// The chat settings assist filter
        /// </summary>
        public const string ChatSettingsAssistFilter = ChatSettings + ".assist_filter";
        /// <summary>
        /// The chat settings clan filter
        /// </summary>
        public const string ChatSettingsClanFilter = ChatSettings + ".clan_filter";
        /// <summary>
        /// The chat settings game filter
        /// </summary>
        public const string ChatSettingsGameFilter = ChatSettings + ".game_filter";
        /// <summary>
        /// The chat settings trade filter
        /// </summary>
        public const string ChatSettingsTradeFilter = ChatSettings + ".trade_filter";
        /// <summary>
        /// The chat settings public filter
        /// </summary>
        public const string ChatSettingsPublicFilter = ChatSettings + ".public_filter";
        /// <summary>
        /// The chat settings private filter
        /// </summary>
        public const string ChatSettingsPrivateFilter = ChatSettings + ".private_filter";
        /// <summary>
        /// The report settings
        /// </summary>
        public const string ReportSettings = "report.settings";
        /// <summary>
        /// The report settings right click toggle
        /// </summary>
        public const string ReportSettingsRightClick = ReportSettings + ".right-click-toggled";
        /// <summary>
        /// The mouse settings
        /// </summary>
        public const string MouseSettings = "mouse.settings";
        /// <summary>
        /// The mouse settings buttons toggle
        /// </summary>
        public const string MouseSettingsButtons = MouseSettings + ".buttons-toggled";
        /// <summary>
        /// The friends chat settings
        /// </summary>
        public const string FriendsChatSettings = "friends_chat.settings";
        /// <summary>
        /// The friends chat settings alias
        /// </summary>
        public const string FriendsChatSettingsAlias = FriendsChatSettings + ".alias";
        /// <summary>
        /// The friends chat settings enter rank
        /// </summary>
        public const string FriendsChatSettingsEnterRank = FriendsChatSettings + ".enter_rank";
        /// <summary>
        /// The friends chat settings talk rank
        /// </summary>
        public const string FriendsChatSettingsTalkRank = FriendsChatSettings + ".talk_rank";
        /// <summary>
        /// The friends chat settings kick rank
        /// </summary>
        public const string FriendsChatSettingsKickRank = FriendsChatSettings + ".kick_rank";
        /// <summary>
        /// The friends chat settings loot rank
        /// </summary>
        public const string FriendsChatSettingsLootRank = FriendsChatSettings + ".loot_rank";
        /// <summary>
        /// The friends chat settings loot share toggled
        /// </summary>
        public const string FriendsChatSettingsLootShareToggled = FriendsChatSettings + ".loot_share_toggled";
    }
}
