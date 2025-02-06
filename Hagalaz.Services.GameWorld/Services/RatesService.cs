using System;
using System.Linq.Expressions;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services
{
    public class RatesService : IRatesService
    {
        private readonly IServiceProvider _servicesProvider;
        private readonly IConfiguration _configuration;

        public RatesService(IServiceProvider servicesProvider, IConfiguration configuration)
        {
            _servicesProvider = servicesProvider;
            _configuration = configuration;
        }

        public double GetRate<TRateOptions>(Expression<Func<TRateOptions, double>> rateSelector) where TRateOptions : class
        {
            ArgumentNullException.ThrowIfNull(rateSelector);

            // Resolve the rate options using the service provider
            var rateOptions = _servicesProvider.GetService<IOptions<TRateOptions>>()?.Value;
            if (rateOptions == null)
            {
                return 1.0;
            }

            // Compile the expression to a delegate and execute it
            var selectorFunc = rateSelector.Compile();
            var result = selectorFunc(rateOptions);

            return result;
        }

        public double GetRate(string configKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(configKey);

            return _configuration.GetValue(configKey, 1.0);
        }
    }
}