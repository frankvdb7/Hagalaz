using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Authorization.Services
{
    public static class OpenIddictServerConfiguration
    {
        public static void ConfigureIssuer(
            OpenIddictServerBuilder options,
            IConfiguration configuration,
            bool isDevelopment)
        {
            var issuer = configuration.GetValue<string>("OpenIddict:Issuer");

            if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
                string.IsNullOrWhiteSpace(issuerUri.Host) ||
                !string.IsNullOrEmpty(issuerUri.Query) ||
                !string.IsNullOrEmpty(issuerUri.Fragment) ||
                (issuerUri.Scheme != Uri.UriSchemeHttps &&
                 (!isDevelopment || issuerUri.Scheme != Uri.UriSchemeHttp)))
            {
                throw new InvalidOperationException(
                    "OpenIddict:Issuer must be configured explicitly as an absolute HTTP(S) URI without a query or fragment." +
                    (isDevelopment ? string.Empty : " Production issuers must use HTTPS."));
            }

            options.SetIssuer(issuerUri);
        }

        public static void ConfigureCredentials(
            OpenIddictServerBuilder options,
            IConfiguration configuration,
            bool isDevelopment)
        {
            if (isDevelopment)
            {
                options.AddDevelopmentSigningCertificate()
                    .AddDevelopmentEncryptionCertificate();
                return;
            }

            options.AddSigningCertificate(
                    OpenIddictCertificateLoader.Load(configuration, "OpenIddict:SigningCertificate"))
                .AddEncryptionCertificate(
                    OpenIddictCertificateLoader.Load(configuration, "OpenIddict:EncryptionCertificate"));
        }
    }
}
