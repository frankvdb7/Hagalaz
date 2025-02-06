namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages
{
    /// <summary>
    ///     Defines the types of return codes send to the hagalaz client at login.
    /// </summary>
    public enum ClientSignInResponseOpcode : byte
    {
        /// <summary>
        ///     The login was successful.
        /// </summary>
        Successful = 2,

        /// <summary>
        ///     The password was invalid / incorrect.
        /// </summary>
        CredentialsInvalid = 3,

        /// <summary>
        ///     The character was disabled.
        /// </summary>
        AccountDisabled = 4,

        /// <summary>
        ///     The character is already online.
        /// </summary>
        AlreadyOnline = 5,

        /// <summary>
        ///     The client is out of date.
        /// </summary>
        OutOfDate = 6,

        /// <summary>
        ///     The world is full.
        /// </summary>
        WorldFull = 7,

        /// <summary>
        ///     Login server is offline.
        /// </summary>
        LoginServerOffline = 8,

        /// <summary>
        ///     The connection limit has exceeded for this ip.
        /// </summary>
        LimitExceeded = 9,

        /// <summary>
        ///     An error happened during loading of account.
        /// </summary>
        BadSession = 10,

        /// <summary>
        ///     The session was rejected.
        /// </summary>
        Rejected = 11,

        MembersOnly = 12,

        CouldNotComplete = 13,

        /// <summary>
        ///     There's a system update.
        /// </summary>
        SystemUpdate = 14,
        TooManyAttempts = 16,
        MembersOnlyArea = 17,
        AccountLocked = 18,
        FullscreenMembersFeature = 19,
        InvalidLoginRequest = 20,
        ProfileTransferring = 21,
        MalformedLoginPacket = 22,
        ErrorLoadingProfile = 24,
        ComputerAddressBlocked = 26,
        ServiceUnavailable = 27,
        NotMembersAccount = 30,
        ChangeAccountDisplayName = 31,
        LoginFailed = 32,
        SessionExpired = 35,
        AuthServerOffline = 36,
        AccountInaccessible = 37,
        DeniedAccessHtml5Beta = 38,
        InstanceNoLongerExists = 39,
        InstanceFull = 41,
        SystemUnavailable = 44,
        MarkedDeletionRebuild = 46,
        ValidateMail = 47,
        FiveMinuteSessionEnded = 48,
        AccountJagEnabled = 50,
        IncompleteLoginUnauthorized = 55,
        Authenticator = 56

    }
}