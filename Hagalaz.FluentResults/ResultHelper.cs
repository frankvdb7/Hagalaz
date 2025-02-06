using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;

namespace Hagalaz.FluentResults
{
    public static class ResultHelper
    {
        public static Result Fail(Exception exception) => Result.Fail(new ExceptionalError(exception));
    }
}
