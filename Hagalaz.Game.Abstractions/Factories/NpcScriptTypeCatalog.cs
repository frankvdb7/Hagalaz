using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Captures script types from an explicitly selected assembly.
    /// </summary>
    public sealed class NpcScriptTypeCatalog : INpcScriptTypeCatalog
    {
        public NpcScriptTypeCatalog(IEnumerable<Type> scriptTypes)
        {
            ArgumentNullException.ThrowIfNull(scriptTypes);
            ScriptTypes = scriptTypes.Where(type => type is not null).Distinct().ToArray();
        }

        public IReadOnlyCollection<Type> ScriptTypes { get; }

        public static NpcScriptTypeCatalog FromAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return new NpcScriptTypeCatalog(assembly.GetTypes());
        }
    }
}
