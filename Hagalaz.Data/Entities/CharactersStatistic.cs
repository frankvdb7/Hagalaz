namespace Hagalaz.Data.Entities
{
    public partial class CharactersStatistic
    {
        public uint MasterId { get; set; }
        public byte AttackLevel { get; set; }
        public int AttackExp { get; set; }
        public byte DefenceLevel { get; set; }
        public int DefenceExp { get; set; }
        public byte StrengthLevel { get; set; }
        public int StrengthExp { get; set; }
        public byte ConstitutionLevel { get; set; }
        public int ConstitutionExp { get; set; }
        public byte RangeLevel { get; set; }
        public int RangeExp { get; set; }
        public byte PrayerLevel { get; set; }
        public int PrayerExp { get; set; }
        public byte MagicLevel { get; set; }
        public int MagicExp { get; set; }
        public byte CookingLevel { get; set; }
        public int CookingExp { get; set; }
        public byte WoodcuttingLevel { get; set; }
        public int WoodcuttingExp { get; set; }
        public byte FletchingLevel { get; set; }
        public int FletchingExp { get; set; }
        public byte FishingLevel { get; set; }
        public int FishingExp { get; set; }
        public byte FiremakingLevel { get; set; }
        public int FiremakingExp { get; set; }
        public byte CraftingLevel { get; set; }
        public int CraftingExp { get; set; }
        public byte SmithingLevel { get; set; }
        public int SmithingExp { get; set; }
        public byte MiningLevel { get; set; }
        public int MiningExp { get; set; }
        public byte HerbloreLevel { get; set; }
        public int HerbloreExp { get; set; }
        public byte AgilityLevel { get; set; }
        public int AgilityExp { get; set; }
        public byte ThievingLevel { get; set; }
        public int ThievingExp { get; set; }
        public byte SlayerLevel { get; set; }
        public int SlayerExp { get; set; }
        public byte FarmingLevel { get; set; }
        public int FarmingExp { get; set; }
        public byte RunecraftingLevel { get; set; }
        public int RunecraftingExp { get; set; }
        public byte ConstructionLevel { get; set; }
        public int ConstructionExp { get; set; }
        public byte HunterLevel { get; set; }
        public int HunterExp { get; set; }
        public byte SummoningLevel { get; set; }
        public int SummoningExp { get; set; }
        public byte DungeoneeringLevel { get; set; }
        public int DungeoneeringExp { get; set; }
        public ushort LifePoints { get; set; }
        public ushort PrayerPoints { get; set; }
        public byte RunEnergy { get; set; }
        public ushort SpecialEnergy { get; set; }
        public ushort PoisonAmount { get; set; }
        public ulong PlayTime { get; set; }
        public string XpCounters { get; set; } = null!;
        public string TrackedXpCounters { get; set; } = null!;
        public string EnabledXpCounters { get; set; } = null!;
        public string TargetSkillLevels { get; set; } = null!;
        public string TargetSkillExperiences { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}
