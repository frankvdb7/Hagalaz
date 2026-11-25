using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Hagalaz.Data.Extensions;
using Microsoft.AspNetCore.Builder;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Hagalaz.Game.Abstractions.Scripts;
using Hagalaz.Services.GameWorld.Providers;
using Raido.Server.Extensions;
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
                var listenHost = builder.Configuration.GetValue<string>("World:ListenHost") ?? IPAddress.Loopback.ToString();
                if (!IPAddress.TryParse(listenHost, out var listenAddress))
                {
                    throw new ArgumentException("World:ListenHost must be a valid IP address.", nameof(listenHost));
                }

                var tcpPort = builder.Configuration.GetValue<int>("TCP_PORT");
                if (tcpPort == 0)
                {
                    throw new ArgumentNullException(nameof(tcpPort));
                }

                options.Listen(listenAddress,
                    tcpPort,
                    listenOptions =>
                    {
                        listenOptions.UseRaido();
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

            await app.RunAsync();
        }

        public static IHostBuilder ConfigurePlugins(this IHostBuilder builder) =>
            builder.ConfigureServices(services =>
            {
                // Create an instance of plugin types
                foreach (var pluginAssembly in PluginAssemblyLoader.LoadAll(Path.Join(AppContext.BaseDirectory, "plugins")))
                {
                    services.AddSingleton(pluginAssembly);

                    foreach (var pluginType in GetLoadableTypes(pluginAssembly)
                                 .Where(t => typeof(IPluginStartup).IsAssignableFrom(t) && !t.IsAbstract))
                    {
                        // This assumes the implementation of IPluginStartup has a parameterless constructor
                        var plugin = Activator.CreateInstance(pluginType) as IPluginStartup;

                        plugin?.Configure(services);
                    }
                }

            });

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                Console.Error.WriteLine($"Plugin assembly '{assembly.FullName}' was only partially loadable: {exception}");
                return exception.Types.OfType<Type>();
            }
        }
    }
}
