using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.NETCore.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Hagalaz.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Connections;
using Hagalaz.Data.Extensions;
using Microsoft.AspNetCore.Builder;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Hagalaz.Data;
using Hagalaz.Game.Abstractions.Scripts;
using Hagalaz.Services.GameWorld.Network;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.ServiceDefaults;

namespace Hagalaz.Services.GameWorld
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var startup = new Startup(builder.Configuration);
            // aspire
            builder.AddServiceDefaults();

            builder.AddHagalazDbContextPool("hagalaz-db");

            startup.ConfigureServices(builder.Services);

            builder.WebHost.UseKestrel().ConfigureKestrel(options =>
            {
                var tcpPort = builder.Configuration.GetValue<int>("TCP_PORT");
                if (tcpPort == 0)
                {
                    throw new ArgumentNullException(nameof(tcpPort));
                }

                options.Listen(IPAddress.Loopback,
                    tcpPort,
                    listenOptions =>
                    {
                        listenOptions.UseConnectionHandler<ClientConnectionHandler>();
                        listenOptions.UseConnectionLogging();
                    });

                var httpsPort = builder.Configuration.GetValue<int>("HTTPS_PORT");
                if (httpsPort == 0)
                {
                    throw new ArgumentNullException(nameof(httpsPort));
                }

                options.Listen(IPAddress.Loopback,
                    httpsPort,
                    listenOptions =>
                    {
                        listenOptions.UseHttps();
                    });

                var httpPort = builder.Configuration.GetValue<int>("HTTP_PORT");
                if (httpPort == 0)
                {
                    throw new ArgumentNullException(nameof(httpPort));
                }

                options.Listen(IPAddress.Loopback, httpPort);
            });
            builder.Host.ConfigurePlugins();

            builder.Services.AddSingleton<IServiceDescriptorProvider>(provider => new ServiceDescriptorProvider(builder.Services));

            var app = builder.Build();

            app.MapDefaultEndpoints();

            startup.Configure(app, app.Environment, app.Services.GetRequiredService<IMapper>());

            await app.MigrateDatabase<HagalazDbContext>();

            ServiceLocator.SetLocatorProvider(app.Services); // TODO - when services are refactored, remove the service locator

            await app.RunAsync();
        }

        private static IEnumerable<PluginLoader> GetPluginLoaders()
        {
            // create plugin loaders
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
            foreach (var dir in Directory.GetDirectories(pluginsDir))
            {
                var dirName = Path.GetFileName(dir);
                var pluginDll = Path.Combine(dir, dirName + ".dll");
                if (!File.Exists(pluginDll))
                {
                    continue;
                }

                var loader = PluginLoader.CreateFromAssemblyFile(pluginDll, config => config.PreferSharedTypes = true);

                yield return loader;
            }
        }

        public static IHostBuilder ConfigurePlugins(this IHostBuilder builder) =>
            builder.ConfigureServices(services =>
            {
                // Create an instance of plugin types
                foreach (var loader in GetPluginLoaders())
                {
                    foreach (var pluginType in loader.LoadDefaultAssembly().GetTypes().Where(t => typeof(IPluginStartup).IsAssignableFrom(t) && !t.IsAbstract))
                    {
                        // This assumes the implementation of IPluginStartup has a parameterless constructor
                        var plugin = Activator.CreateInstance(pluginType) as IPluginStartup;

                        plugin?.Configure(services);
                    }
                }
            });
    }
}