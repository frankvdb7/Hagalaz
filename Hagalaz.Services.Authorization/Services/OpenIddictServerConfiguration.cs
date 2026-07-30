using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Authorization.Services
{
    public static class OpenIddictServerConfiguration
    {
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
