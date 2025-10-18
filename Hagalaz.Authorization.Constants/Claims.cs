namespace Hagalaz.Authorization.Constants;

/// <summary>
/// Defines the constant values for claim types used in the application's identity and authorization system.
/// </summary>
public static class Claims
{
    /// <summary>
    /// The claim type for the timestamp of the user's last login.
    /// </summary>
    public const string LastLogin = "last_login";

    /// <summary>
    /// The claim type for the IP address of the user's last login.
    /// </summary>
    public const string LastIp = "last_ip";

    /// <summary>
    /// The claim type for the user's previous preferred username, used for tracking username changes.
    /// </summary>
    public const string PreviousPreferredUsername = "previous_preferred_username";
}