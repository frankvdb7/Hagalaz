using System;
using System.Linq.Expressions;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that provides access to various game rates, such as experience and drop rate multipliers.
    /// </summary>
    public interface IRatesService
    {
        /// <summary>
        /// Gets a rate value using a strongly-typed expression.
        /// </summary>
        /// <typeparam name="TRateOptions">The type of the options class containing the rate.</typeparam>
        /// <param name="rateSelector">An expression that selects the rate property from the options class.</param>
        /// <returns>The rate value.</returns>
        public double GetRate<TRateOptions>(Expression<Func<TRateOptions, double>> rateSelector) where TRateOptions : class;

        /// <summary>
        /// Gets a rate value using its configuration key.
        /// </summary>
        /// <param name="configKey">The configuration key for the rate.</param>
        /// <returns>The rate value.</returns>
        public double GetRate(string configKey);
    }
}