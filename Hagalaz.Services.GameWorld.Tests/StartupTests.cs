using Hagalaz.Services.GameWorld.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Data.Extensions;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public sealed class StartupTests
    {
        [TestMethod]
        public void BuildApp()
        {
            var builder = WebApplication.CreateBuilder([]);
            builder.AddHagalazDbContextPool("test-db");
            builder.Services.AddSingleton<IServiceDescriptorProvider>(provider => new ServiceDescriptorProvider(builder.Services));
            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);
            builder.Host.ConfigurePlugins();
            builder.Build();
        }
    }
}