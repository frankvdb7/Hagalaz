namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedStatisticsDto
    {
        public int AttackLevel { get; init; }
        public double AttackExp { get; init; }
        public int DefenceLevel { get; init; }
        public double DefenceExp { get; init; }
        public int StrengthLevel { get; init; }
        public double StrengthExp { get; init; }
        public int ConstitutionLevel { get; init; }
        public double ConstitutionExp { get; init; }
        public int RangeLevel { get; init; }
        public double RangeExp { get; init; }
        public int PrayerLevel { get; init; }
        public double PrayerExp { get; init; }
        public int MagicLevel { get; init; }
        public double MagicExp { get; init; }
        public int CookingLevel { get; init; }
        public double CookingExp { get; init; }
        public int WoodcuttingLevel { get; init; }
        public double WoodcuttingExp { get; init; }
        public int FletchingLevel { get; init; }
        public double FletchingExp { get; init; }
        public int FishingLevel { get; init; }
        public double FishingExp { get; init; }
        public int FiremakingLevel { get; init; }
        public double FiremakingExp { get; init; }
        public int CraftingLevel { get; init; }
        public double CraftingExp { get; init; }
        public int SmithingLevel { get; init; }
        public double SmithingExp { get; init; }
        public int MiningLevel { get; init; }
        public double MiningExp { get; init; }
        public int HerbloreLevel { get; init; }
        public double HerbloreExp { get; init; }
        public int AgilityLevel { get; init; }
        public double AgilityExp { get; init; }
        public int ThievingLevel { get; init; }
        public double ThievingExp { get; init; }
        public int SlayerLevel { get; init; }
        public double SlayerExp { get; init; }
        public int FarmingLevel { get; init; }
        public double FarmingExp { get; init; }
        public int RunecraftingLevel { get; init; }
        public double RunecraftingExp { get; init; }
        public int ConstructionLevel { get; init; }
        public double ConstructionExp { get; init; }
        public int HunterLevel { get; init; }
        public double HunterExp { get; init; }
        public int SummoningLevel { get; init; }
        public double SummoningExp { get; init; }
        public int DungeoneeringLevel { get; init; }
        public double DungeoneeringExp { get; init; }
        public int LifePoints { get; init; }
        public int PrayerPoints { get; init; }
        public int RunEnergy { get; init; }
        public int SpecialEnergy { get; init; }
        public int PoisonAmount { get; init; }
        public long PlayTime { get; init; }
        public int[] XpCounters { get; init; } = [];
        public int[] TrackedXpCounters { get; init; } = [];
        public bool[] EnabledXpCounters { get; init; } = [];
        public int[] TargetSkillLevels { get; init; } = [];
        public double[] TargetSkillExperiences { get; init; } = [];
    }
}
