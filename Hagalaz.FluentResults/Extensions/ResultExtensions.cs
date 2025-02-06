using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Hagalaz.FluentResults.Extensions
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Aggregates all of the Exceptions from all of the reasons behind all of the errors.
        /// </summary>
        /// <param name="error">When your <see cref="Result"/> ends in failure.</param>
        /// <returns>All of the exceptions that happened when processing the <see cref="Result"/></returns>
        public static AggregateException Exceptions<T>(this Result<T> error)
        {
            var exceptionalErrors = error.Errors.SelectMany(err => err.Reasons.OfType<ExceptionalError>())
                .Select(ex => ex.Exception);

            return new AggregateException(exceptionalErrors);
        }

        public static bool HasException<TException>(this Result result) where TException : Exception
        {
            var exceptionType = typeof(TException);
            return result.Errors.SelectMany(err => err.Reasons.OfType<ExceptionalError>())
                .Any(err => err.Exception.GetType() == exceptionType);
        }
    }
}
