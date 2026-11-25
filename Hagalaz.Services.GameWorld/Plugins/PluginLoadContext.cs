using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Hagalaz.Services.GameWorld.Plugins
{
    /// <summary>
    /// Non-collectible load context for a single plugin. Managed dependencies are resolved host-first so that
    /// shared Hagalaz/framework assemblies keep a single type identity; anything the host cannot resolve comes
    /// from the plugin's own dependency graph.
    /// </summary>
    internal sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginAssemblyPath)
            : base(Path.GetFileNameWithoutExtension(pluginAssemblyPath), isCollectible: false) =>
            _resolver = new AssemblyDependencyResolver(pluginAssemblyPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var hostAssembly = TryLoadFromHost(assemblyName);
            if (hostAssembly is not null)
            {
                return hostAssembly;
            }

            var pluginPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return pluginPath is null ? null : LoadFromAssemblyPath(pluginPath);
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
        }

        private static Assembly? TryLoadFromHost(AssemblyName assemblyName)
        {
            try
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                // The host dependency graph does not contain this assembly; the plugin supplies it.
                return null;
            }
        }
    }
}
