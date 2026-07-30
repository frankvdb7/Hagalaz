namespace Hagalaz.Services.Authorization.Config
{
    /// <summary>
    /// Describes a PKCS#12 certificate used by the OpenIddict server.
    /// </summary>
    public sealed class OpenIddictCertificateOptions
    {
        public string? Path { get; set; }

        public string? Password { get; set; }
    }
}
