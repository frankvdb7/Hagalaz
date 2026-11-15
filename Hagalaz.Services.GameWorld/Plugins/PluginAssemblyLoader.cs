using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Hagalaz.Services.GameWorld.Plugins
{
    internal static class PluginAssemblyLoader
    {
        /// <summary>
        /// Loads every <c>&lt;pluginsDirectory&gt;/&lt;name&gt;/&lt;name&gt;.dll</c> plugin assembly.
        /// </summary>
        public static IEnumerable<Assembly> LoadAll(string pluginsDirectory)
        {
            foreach (var dir in Directory.GetDirectories(pluginsDirectory))
            {
                var pluginDll = Path.Combine(dir, Path.GetFileName(dir) + ".dll");
                if (!File.Exists(pluginDll))
                {
                    continue;
                }

                yield return Load(pluginDll);
            }
        }

        public static Assembly Load(string pluginAssemblyPath) =>
            new PluginLoadContext(pluginAssemblyPath).LoadFromAssemblyPath(Path.GetFullPath(pluginAssemblyPath));
    }
}
