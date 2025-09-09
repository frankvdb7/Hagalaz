namespace Hagalaz.Game.Abstractions.Logic.Random
{
    public interface IRandomProvider
    {
        int Next(int maxValue);
        int Next(int minValue, int maxValue);
        double NextDouble();
        double Next(double maxValue);
    }
}
