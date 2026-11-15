using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Hagalaz.Game.Abstractions.Scripts;
using Hagalaz.Services.GameWorld.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Tests.Plugins
{
    [TestClass]
    public class PluginAssemblyLoaderTests
    {
        public TestContext TestContext { get; set; } = null!;

        private static string PluginsDirectory => Path.Join(AppContext.BaseDirectory, "plugins");

        private static Assembly LoadScripts() => PluginAssemblyLoader.LoadAll(PluginsDirectory).Single(a => a.GetName().Name == "Hagalaz.Game.Scripts");

        [TestMethod]
        public void LoadAll_DiscoversPluginUnderBaseDirectoryPluginsFolder()
        {
            var assembly = LoadScripts();

            Assert.AreEqual(Path.Join(PluginsDirectory, "Hagalaz.Game.Scripts", "Hagalaz.Game.Scripts.dll"), assembly.Location);
            Assert.AreNotSame(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(assembly));
            Assert.IsFalse(AssemblyLoadContext.GetLoadContext(assembly)!.IsCollectible);
        }

        [TestMethod]
        public void LoadedStartup_IsAssignableToHostPluginStartup()
        {
            var startupType = LoadScripts().GetTypes().Single(t => typeof(IPluginStartup).IsAssignableFrom(t) && !t.IsAbstract);

            Assert.AreEqual("Hagalaz.Game.Scripts.Startup", startupType.FullName);
            Assert.IsTrue(typeof(IPluginStartup).IsAssignableFrom(startupType));
        }

        [TestMethod]
        public void SharedHagalazAndFrameworkAssemblies_ResolveToHostIdentity()
        {
            var assembly = LoadScripts();
            var context = AssemblyLoadContext.GetLoadContext(assembly)!;

            var abstractions = context.LoadFromAssemblyName(typeof(IPluginStartup).Assembly.GetName());
            var di = context.LoadFromAssemblyName(typeof(IServiceCollection).Assembly.GetName());

            Assert.AreSame(typeof(IPluginStartup).Assembly, abstractions);
            Assert.AreSame(typeof(IServiceCollection).Assembly, di);
            Assert.IsFalse(context.Assemblies.Any(a => a.GetName().Name == typeof(IPluginStartup).Assembly.GetName().Name));
        }

        [TestMethod]
        public void Startup_Configure_RegistersScriptServices()
        {
            var startupType = LoadScripts().GetTypes().Single(t => typeof(IPluginStartup).IsAssignableFrom(t) && !t.IsAbstract);
            var services = new ServiceCollection();

            ((IPluginStartup)Activator.CreateInstance(startupType)!).Configure(services);

            Assert.IsTrue(services.Count > 0);
            Assert.IsTrue(services.Any(d => d.ServiceType.Name == "IDefaultNpcScriptProvider"));
        }

        [TestMethod]
        public void PluginPrivateDependency_ResolvesFromPluginDirectory()
        {
            var dir = Path.Join(Path.GetTempPath(), "hagalaz-plugin-" + Guid.NewGuid().ToString("N"), "Fixture");
            Directory.CreateDirectory(dir);
            try
            {
                EmitAssembly("Fixture", Path.Join(dir, "Fixture.dll"));
                EmitAssembly("FixturePrivateDependency", Path.Join(dir, "FixturePrivateDependency.dll"));

                var plugin = PluginAssemblyLoader.Load(Path.Join(dir, "Fixture.dll"));
                var context = AssemblyLoadContext.GetLoadContext(plugin)!;
                var dependency = context.LoadFromAssemblyName(new AssemblyName("FixturePrivateDependency"));

                Assert.AreSame(context, AssemblyLoadContext.GetLoadContext(dependency));
                Assert.AreEqual(Path.Join(dir, "FixturePrivateDependency.dll"), dependency.Location);
            }
            finally
            {
                // Non-collectible contexts keep the loaded files locked on Windows; cleanup is best-effort.
                try
                {
                    Directory.Delete(Path.GetDirectoryName(dir)!, recursive: true);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    TestContext.WriteLine($"Could not delete fixture directory: {ex.Message}");
                }
            }
        }

        private static void EmitAssembly(string name, string path)
        {
            var builder = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
            builder.DefineDynamicModule(name).DefineType(name + ".Marker", TypeAttributes.Public | TypeAttributes.Class).CreateType();
            builder.Save(path);
        }
    }
}
