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
            var actualHits = hits.Where(h => h > 0).ToArray();
            return actualHits.Any() ? actualHits.Sum() : -1;
        }

        /// <summary>
        ///     Calculate's amount of ticks for client ticks.
        /// </summary>
        /// <param name="clientTicks">The client ticks.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculateTicksForClientTicks(int clientTicks) => (clientTicks * 20 + 599) / 600;
    }
}