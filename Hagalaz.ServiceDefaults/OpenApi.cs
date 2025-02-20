using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Hagalaz.ServiceDefaults
{
    internal static class OpenApi
    {
        internal sealed class OpenIdConnectSecuritySchemeTransformer(IConfiguration configuration, ILogger<OpenIdConnectSecuritySchemeTransformer> logger) : IOpenApiDocumentTransformer
        {
            public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                var authServiceUri = configuration.GetServiceConfigurationValue("hagalaz-services-authorization", "https", "http");
                if (string.IsNullOrEmpty(authServiceUri))
                {
                    logger.LogError("The swagger authentication URI is not configured");
                    return;
                }
                var securitySchema =
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Provides OAuth2 api access",
                        Type = SecuritySchemeType.OAuth2, // TODO: Change to OpenIdConnect if Scalar starts supporting it
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        Flows = new OpenApiOAuthFlows
                        {
                            Password = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri($"{authServiceUri}/connect/authorize"),
                                TokenUrl = new Uri($"{authServiceUri}/connect/token"),
                                Scopes = new Dictionary<string, string>
                                {
                                    {"openid", "OpenId"},
                                    {"profile", "Profile"},
                                    {"email", "Email"},
                                    {"offline_access", "Offline Access"},
                                },
                            }
                        },
                        OpenIdConnectUrl = new Uri($"{authServiceUri}/.well-known/openid-configuration"),
                    };

                var securityRequirement =
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer", Type = ReferenceType.SecurityScheme,
                                },
                            },
                            []
                        }
                    };

                document.SecurityRequirements.Add(securityRequirement);
                document.Components = new OpenApiComponents()
                {
                    SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>()
                    {
                        {
                            "Bearer", securitySchema
                        }
                    }
                };
            }
        }
    }
}