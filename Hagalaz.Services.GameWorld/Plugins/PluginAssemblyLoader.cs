using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hagalaz.Services.GameWorld.Plugins
{
    internal static class PluginAssemblyLoader
    {
        /// <summary>
        /// Loads every <c>&lt;pluginsDirectory&gt;/&lt;name&gt;/&lt;name&gt;.dll</c> plugin assembly.
        /// </summary>
        public static IEnumerable<Assembly> LoadAll(string pluginsDirectory) =>
            Directory.GetDirectories(pluginsDirectory)
                .Select(dir => Path.Join(dir, Path.GetFileName(dir) + ".dll"))
                .Where(File.Exists)
                .Select(Load);

        public static Assembly Load(string pluginAssemblyPath) =>
            new PluginLoadContext(pluginAssemblyPath).LoadFromAssemblyPath(Path.GetFullPath(pluginAssemblyPath));
    }
}
