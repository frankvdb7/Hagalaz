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
            var predicted = -1; // miss
            for (var i = 0; i < hits.Length; i++)
            {
                if (i == -1)
                {
                    continue;
                }

                if (predicted == -1)
                {
                    predicted = i;
                }
                else
                {
                    predicted += i;
                }
            }

            return predicted;
        }

        /// <summary>
        ///     Calculate's amount of ticks for client ticks.
        /// </summary>
        /// <param name="clientTicks">The client ticks.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculateTicksForClientTicks(int clientTicks) => (clientTicks * 20 + 599) / 600;
    }
}