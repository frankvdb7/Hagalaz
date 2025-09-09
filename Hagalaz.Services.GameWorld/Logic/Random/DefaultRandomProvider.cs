using Hagalaz.Game.Abstractions.Logic.Random;
using System;

namespace Hagalaz.Services.GameWorld.Logic.Random
{
    public class DefaultRandomProvider : IRandomProvider
    {
        public int Next(int maxValue) => System.Random.Shared.Next(maxValue);
        public int Next(int minValue, int maxValue) => System.Random.Shared.Next(minValue, maxValue);
        public double NextDouble() => System.Random.Shared.NextDouble();
    }
}
