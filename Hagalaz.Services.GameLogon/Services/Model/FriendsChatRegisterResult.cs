namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record FriendsChatRegisterResult
    {
        public static FriendsChatRegisterResult Success { get; } = new()
        {
            Succeeded = true
        };

        public static FriendsChatRegisterResult Fail { get; } = new();

        public static FriendsChatRegisterResult NotFound { get; } = new()
        {
            IsNotFound = true
        };

        public static FriendsChatRegisterResult Full { get; } = new()
        {
            IsFull = true
        };

        public static FriendsChatRegisterResult Unauthorized { get; } = new()
        {
            IsUnauthorized = true
        };

        public static FriendsChatRegisterResult Banned { get; } = new()
        {
            IsBanned = true
        };


        public bool Succeeded { get; private init; }
        public bool IsNotFound { get; private init; }
        public bool IsFull { get; private init; }
        public bool IsUnauthorized { get; private init; }
        public bool IsBanned { get; private init; }

        private FriendsChatRegisterResult()
        {
        }
    }
}