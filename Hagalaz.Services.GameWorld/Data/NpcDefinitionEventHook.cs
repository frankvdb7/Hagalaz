using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Data
{
    public class NpcDefinitionEventHook : ITypeEventHook<INpcDefinition>
    {
        public void AfterDecode(ITypeDecoder<INpcDefinition> decoder, INpcDefinition[] types)
        {
            foreach (var type in types)
            {
                type.DisplayName = type.Name;
            }
        }
    }
}
