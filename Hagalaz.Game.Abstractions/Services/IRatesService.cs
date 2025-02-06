using System;
using System.Linq.Expressions;

namespace Hagalaz.Game.Abstractions.Services
{
    public interface IRatesService
    {
        public double GetRate<TRateOptions>(Expression<Func<TRateOptions, double>> rateSelector) where TRateOptions : class;
        public double GetRate(string configKey);
    }
}