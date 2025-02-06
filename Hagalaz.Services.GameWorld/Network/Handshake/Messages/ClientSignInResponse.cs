using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages;

public class ClientSignInResponse : RaidoMessage
{
    public static ClientSignInResponse Success { get; } = new()
    {
        Succeeded = true
    };

    public static ClientSignInResponse Failed { get; } = new()
    {
        Succeeded = false
    };

    public static ClientSignInResponse CredentialsInvalid { get; } = new()
    {
        AreCredentialsInvalid = true
    };

    public static ClientSignInResponse Outdated { get; } = new()
    {
        IsOutdated = true
    };

    public static ClientSignInResponse BadSession { get; } = new()
    {
        IsBadSession = true
    };

    public static ClientSignInResponse SystemUpdate { get; } = new()
    {
        IsSystemUpdate = true
    };

    public static ClientSignInResponse AuthServiceOffline { get; } = new()
    {
        IsServerOffline = true
    };

    public static ClientSignInResponse AlreadyLoggedOn { get; } = new()
    {
        IsAlreadyLoggedOn = true
    };

    public static ClientSignInResponse Disabled { get; } = new()
    {
        IsDisabled = true
    };

    public static ClientSignInResponse Full { get; } = new()
    {
        IsFull = true
    };

    public static ClientSignInResponse LockedOut { get; } = new()
    {
        IsLockedOut = true
    };

    public bool Succeeded { get; protected init; }
    public bool AreCredentialsInvalid { get; private init; }
    public bool IsBadSession { get; private init; }
    public bool IsOutdated { get; private init; }
    public bool IsSystemUpdate { get; private init; }
    public bool IsServerOffline { get; private init; }
    public bool IsAlreadyLoggedOn { get; private init; }
    public bool IsDisabled { get; private init; }
    public bool IsFull { get; private init; }
    public bool IsLockedOut { get; private init; }
}

public static class HandshakeResponseBaseExtensions
{
    public static byte GetOpcode(this ClientSignInResponse response)
    {
        ClientSignInResponseOpcode opcode;
        if (response.Succeeded)
        {
            opcode = ClientSignInResponseOpcode.Successful;
        }
        else if (response.IsFull)
        {
            opcode = ClientSignInResponseOpcode.WorldFull;
        }
        else if (response.IsOutdated)
        {
            opcode = ClientSignInResponseOpcode.OutOfDate;
        }
        else if (response.IsDisabled)
        {
            opcode = ClientSignInResponseOpcode.AccountDisabled;
        }
        else if (response.AreCredentialsInvalid)
        {
            opcode = ClientSignInResponseOpcode.CredentialsInvalid;
        }
        else if (response.IsServerOffline)
        {
            opcode = ClientSignInResponseOpcode.LoginServerOffline;
        }
        else if (response.IsAlreadyLoggedOn)
        {
            opcode = ClientSignInResponseOpcode.AlreadyOnline;
        }
        else if (response.IsSystemUpdate)
        {
            opcode = ClientSignInResponseOpcode.SystemUpdate;
        } 
        else if (response.IsLockedOut)
        {
            opcode = ClientSignInResponseOpcode.TooManyAttempts;
        }
        else
        {
            opcode = ClientSignInResponseOpcode.BadSession;
        }

        return (byte)opcode;
    }
}