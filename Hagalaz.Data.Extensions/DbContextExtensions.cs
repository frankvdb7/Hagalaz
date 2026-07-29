using System;
using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Hagalaz.Data.Extensions
{
    public static class DbContextExtensions
    {
        public static IHostApplicationBuilder AddHagalazDbContextPool(this IHostApplicationBuilder builder, string connectionName)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

            var connectionString = builder.Configuration.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"The database connection string '{connectionName}' was not configured.");
            }

            builder.Services.AddHagalazIdentity();
            var oracleConnectionString = NormalizeOracleConnectionString(connectionString, connectionName);
            builder.Services.AddDbContextPool<HagalazDbContext>(options =>
            {
                options.UseMySQL(oracleConnectionString, mysqlOptions => mysqlOptions.EnableRetryOnFailure(6));
                options.UseLazyLoadingProxies();
                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict();
            });
            builder.Services.AddHealthChecks().AddDbContextCheck<HagalazDbContext>();

            return builder;
        }

        private static string NormalizeOracleConnectionString(string connectionString, string connectionName)
        {
            try
            {
                var connectionStringBuilder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                if (connectionStringBuilder.TryGetValue("SslMode", out var sslMode) &&
                    string.Equals(Convert.ToString(sslMode), "None", StringComparison.OrdinalIgnoreCase))
                {
                    // The previous provider used None for an unencrypted connection. Connector/NET
                    // uses Disabled for the same setting and rejects None before EF can start.
                    connectionStringBuilder["SslMode"] = "Disabled";
                    return connectionStringBuilder.ConnectionString;
                }

                return connectionString;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"The database connection string '{connectionName}' is invalid. " +
                    "Verify its key/value syntax before starting the service.",
                    exception);
            }
        }

        public static IServiceCollection AddHagalazIdentity(this IServiceCollection services)
        {
            services.TryAddScoped<IPasswordHasher<Character>, HagalazPasswordHasher>();
            services.Configure<IdentityOptions>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                })
                .AddIdentity<Character, Aspnetrole>()
                .AddEntityFrameworkStores<HagalazDbContext>();
            return services;
        }

        public static OpenIddictBuilder AddHagalazOpenIddictCore(this OpenIddictBuilder builder, Action<OpenIddictCoreBuilder> configuration) =>
            builder.AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .DisableBulkOperations()
                    .UseDbContext<HagalazDbContext>();
                configuration(options);
            });
    }
}
