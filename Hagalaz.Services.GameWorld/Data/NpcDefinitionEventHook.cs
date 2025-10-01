using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcDefinitionEventHook : ITypeEventHook<INpcDefinition>
    {
        public void AfterDecode(ITypeProvider<INpcDefinition> provider, INpcDefinition[] types)
        {
            foreach (var type in types)
            {
                type.DisplayName = type.Name;
            }
        }
    }
}
