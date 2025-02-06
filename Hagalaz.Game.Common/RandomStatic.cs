using System;

namespace Hagalaz.Game.Common
{
    /// <summary>
    ///
    /// </summary>
    public static class RandomStatic
    {
        /// <summary>
        /// Get the random number generator.
        /// </summary>
        /// <value>Random.</value>
        public static Random Generator => Random.Shared;
    }
}