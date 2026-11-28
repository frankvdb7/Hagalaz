using FluentResults;

namespace Hagalaz.FluentResults
{
    public static class ResultHelper
    {
        public static Result Fail(Exception exception) => Result.Fail(new ExceptionalError(exception));
    }
}
