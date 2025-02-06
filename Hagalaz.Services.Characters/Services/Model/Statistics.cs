namespace Hagalaz.Services.Characters.Services.Model
{
    public record Statistics
    {
        public int AttackLevel { get; init; }
        public int AttackExp { get; init; }
        public int DefenceLevel { get; init; }
        public int DefenceExp { get; init; }
        public int StrengthLevel { get; init; }
        public int StrengthExp { get; init; }
        public int ConstitutionLevel { get; init; }
        public int ConstitutionExp { get; init; }
        public int RangeLevel { get; init; }
        public int RangeExp { get; init; }
        public int PrayerLevel { get; init; }
        public int PrayerExp { get; init; }
        public int MagicLevel { get; init; }
        public int MagicExp { get; init; }
        public int CookingLevel { get; init; }
        public int CookingExp { get; init; }
        public int WoodcuttingLevel { get; init; }
        public int WoodcuttingExp { get; init; }
        public int FletchingLevel { get; init; }
        public int FletchingExp { get; init; }
        public int FishingLevel { get; init; }
        public int FishingExp { get; init; }
        public int FiremakingLevel { get; init; }
        public int FiremakingExp { get; init; }
        public int CraftingLevel { get; init; }
        public int CraftingExp { get; init; }
        public int SmithingLevel { get; init; }
        public int SmithingExp { get; init; }
        public int MiningLevel { get; init; }
        public int MiningExp { get; init; }
        public int HerbloreLevel { get; init; }
        public int HerbloreExp { get; init; }
        public int AgilityLevel { get; init; }
        public int AgilityExp { get; init; }
        public int ThievingLevel { get; init; }
        public int ThievingExp { get; init; }
        public int SlayerLevel { get; init; }
        public int SlayerExp { get; init; }
        public int FarmingLevel { get; init; }
        public int FarmingExp { get; init; }
        public int RunecraftingLevel { get; init; }
        public int RunecraftingExp { get; init; }
        public int ConstructionLevel { get; init; }
        public int ConstructionExp { get; init; }
        public int HunterLevel { get; init; }
        public int HunterExp { get; init; }
        public int SummoningLevel { get; init; }
        public int SummoningExp { get; init; }
        public int DungeoneeringLevel { get; init; }
        public int DungeoneeringExp { get; init; }
        public int LifePoints { get; init; }
        public int PrayerPoints { get; init; }
        public int RunEnergy { get; init; }
        public int SpecialEnergy { get; init; }
        public int PoisonAmount { get; init; }
        public long PlayTime { get; init; }
        public required int[] XpCounters { get; init; }
        public required int[] TrackedXpCounters { get; init; }
        public required bool[] EnabledXpCounters { get; init; }
        public required int[] TargetSkillLevels { get; init; }
        public required double[] TargetSkillExperiences { get; init; }
    }
}
