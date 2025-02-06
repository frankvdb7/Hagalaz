namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record SignInResult
    {
        public static SignInResult Success => new()
        {
            Succeeded = true
        };

        public static SignInResult CredentialsInvalid { get; } = new()
        {
            AreCredentialsInvalid = true
        };

        public static SignInResult Fail { get; } = new();

        public static SignInResult AlreadyLoggedOn { get; } = new()
        {
            IsAlreadyLoggedOn = true
        };

        public static SignInResult Disabled { get; } = new()
        {
            IsDisabled = true
        };

        public static SignInResult LockedOut { get; } = new()
        {
            IsLockedOut = true
        };

        public static SignInResult Full { get; } = new()
        {
            IsFull = true
        };

        public bool Succeeded { get; private init; }
        public bool IsAlreadyLoggedOn { get; private init; }
        public bool IsDisabled { get; private init; }
        public bool AreCredentialsInvalid { get; private init; }
        public bool IsLockedOut { get; private init; }
        public bool IsFull { get; private init; }


        private SignInResult() { }
    }
}