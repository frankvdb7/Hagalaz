namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record Result
    {
        public static Result Success { get; } = new()
        {
            Succeeded = true
        };

        public static Result Fail { get; } = new();

        public static Result NotFound { get; } = new()
        {
            IsNotFound = true
        };

        public bool Succeeded { get; private init; }
        public bool IsNotFound { get; private init; }

        private Result()
        {
        }
    }
}