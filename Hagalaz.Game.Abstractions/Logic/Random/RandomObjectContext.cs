namespace Hagalaz.Game.Abstractions.Logic.Random
{
    public record RandomObjectContext(IRandomObject Object)
    {
        public double BaseProbability { get; init; } = Object.Probability;
        public double ModifiedProbability { get; set; } = Object.Probability;
    }
}