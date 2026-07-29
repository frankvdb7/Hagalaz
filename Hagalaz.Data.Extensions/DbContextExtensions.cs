using System;
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
            builder.Services.AddDbContextPool<HagalazDbContext>(options =>
            {
                options.UseMySQL(connectionString, mysqlOptions => mysqlOptions.EnableRetryOnFailure(6));
                options.UseLazyLoadingProxies();
                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                options.UseOpenIddict();
            });
            builder.Services.AddHealthChecks().AddDbContextCheck<HagalazDbContext>();

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
                options.UseEntityFrameworkCore()
                    .DisableBulkOperations()
                    .UseDbContext<HagalazDbContext>();
                configuration(options);
            });
    }
}
