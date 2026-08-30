namespace Hagalaz.Services.GameWorld.Services.Model;

public record WorldReconnectAuthenticationResult
{
    public static WorldReconnectAuthenticationResult Fail { get; } = new();

    public bool Succeeded { get; private init; }
    public uint? MasterId { get; private init; }
    public bool IsLockedOut { get; private init; }
    public bool IsDisabled { get; private init; }
    public bool AreCredentialsInvalid { get; private init; }
    public bool IsNotAllowed { get; private init; }

    public static WorldReconnectAuthenticationResult Success(uint masterId) => new()
    {
        Succeeded = true,
        MasterId = masterId
    };

    public static WorldReconnectAuthenticationResult FromValidation(
        bool isLockedOut,
        bool isDisabled,
        bool areCredentialsInvalid,
        bool isNotAllowed) => new()
        {
            IsLockedOut = isLockedOut,
            IsDisabled = isDisabled,
            AreCredentialsInvalid = areCredentialsInvalid,
            IsNotAllowed = isNotAllowed
        };
}
