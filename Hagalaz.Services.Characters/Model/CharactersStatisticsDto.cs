namespace Hagalaz.Services.Characters.Model
{
    public record CharactersStatisticsDto
    {
        public required string DisplayName { get; init; }

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
        
        public int OverallLevel =>
            AgilityLevel + AttackLevel + ConstitutionLevel + ConstructionLevel + CookingLevel + CraftingLevel + DefenceLevel + DungeoneeringLevel +
            FarmingLevel + FiremakingLevel + FishingLevel + FletchingLevel + HerbloreLevel + HunterLevel + MagicLevel + MiningLevel + PrayerLevel + RangeLevel +
            RunecraftingLevel + SlayerLevel + SmithingLevel + StrengthLevel + SummoningLevel + ThievingLevel + WoodcuttingLevel;

        public double OverallExperience =>
            AgilityExp + AttackExp + ConstitutionExp + ConstructionExp + CookingExp + CraftingExp + DefenceExp + DefenceExp + DungeoneeringExp + FarmingExp +
            FiremakingExp + FishingExp + FletchingExp + HerbloreExp + HunterExp + MagicExp + MiningExp + PrayerExp + RangeExp + RunecraftingExp + SlayerExp +
            SmithingExp + StrengthExp + SummoningExp + ThievingExp + WoodcuttingExp;
    }
}