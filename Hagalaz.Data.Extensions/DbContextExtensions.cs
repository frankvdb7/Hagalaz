using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hagalaz.Data.Entities;
using Hagalaz.Data.Users;
using Aspire.Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;

namespace Hagalaz.Data.Extensions
{
    public static class DbContextExtensions
    {
        public static IHostApplicationBuilder AddHagalazDbContextPool(this IHostApplicationBuilder builder, string connectionName)
        {
            builder.Services.AddHagalazIdentity();
            builder.AddMySqlDbContext<HagalazDbContext>(connectionName,
                settings =>
                {
                    settings.ServerVersion = MySqlServerVersion.LatestSupportedServerVersion.ToString();
#if DEBUG
                    settings.DisableTracing = false;
#endif
                    settings.DisableMetrics = false;
                    settings.DisableHealthChecks = false;
                    settings.DisableRetry = false;
                },
                options =>
                {
                    var extension = options.Options.FindExtension<MySqlOptionsExtension>();
                    if (extension != null)
                    {
                        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(extension.WithPrimitiveCollectionsSupport(enable: true)
                            .WithParameterizedCollectionTranslationMode(ParameterizedCollectionTranslationMode.Constantize));
                    }

                    options.UseLazyLoadingProxies();
                    // Register the entity sets needed by OpenIddict.
                    // Note: use the generic overload if you need
                    // to replace the default OpenIddict entities.
                    options.UseOpenIddict();
                });

            return builder;
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
                // https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1903
                options.UseEntityFrameworkCore()
                    .DisableBulkOperations()
                    .UseDbContext<HagalazDbContext>();
                configuration(options);
            });
    }
}