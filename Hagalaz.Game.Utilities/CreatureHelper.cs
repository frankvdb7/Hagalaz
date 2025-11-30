using System.Linq;

namespace Hagalaz.Game.Utilities
{
    public static class CreatureHelper
    {
        /// <summary>
        ///     Calculate's predicted damage from one or more hits.
        /// </summary>
        /// <param name="hits">The hits.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculatePredictedDamage(int[] hits)
        {
            if (hits is null || !hits.Any())
            {
                return -1;
            }

            if (hits.All(h => h == 0))
            {
                return 0;
            }

            var actualHits = hits.Where(h => h > 0).Select(h => (long)h).ToArray();
            var sum = actualHits.Sum();

            return sum > 0 ? (int)Math.Min(sum, int.MaxValue) : -1;
        }

        /// <summary>
        ///     Calculate's amount of ticks for client ticks.
        /// </summary>
        /// <param name="clientTicks">The client ticks.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculateTicksForClientTicks(int clientTicks) => (int)((clientTicks * 20L + 599) / 600);
    }
}
