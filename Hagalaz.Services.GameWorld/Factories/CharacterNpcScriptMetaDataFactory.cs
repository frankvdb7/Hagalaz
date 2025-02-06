using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Providers;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class CharacterNpcScriptMetaDataFactory : ICharacterNpcScriptFactory
    {
        private readonly IServiceDescriptorProvider _serviceDescriptorProvider;

        public CharacterNpcScriptMetaDataFactory(IServiceDescriptorProvider serviceDescriptorProvider)
        {
            _serviceDescriptorProvider = serviceDescriptorProvider;
        }

        public async IAsyncEnumerable<(int npcId, Type scriptType)> GetScripts()
        {
            await Task.CompletedTask;
            var type = typeof(ICharacterNpcScript);
            var scriptTypes = _serviceDescriptorProvider.GetServiceDescriptors()
                .Where(x => x.ServiceType.IsAssignableTo(type))
                .Select(x => (ScriptType: x.ImplementationType, MetaData: x.ImplementationType?.GetCustomAttribute<CharacterNpcScriptMetaData>()));

            foreach (var (scriptType, metaData) in scriptTypes)
            {
                if (scriptType is null || metaData is null)
                {
                    continue;
                }

                foreach (var npcId in metaData.NpcIds)
                {
                    yield return (npcId, scriptType);
                }
            }
        }
    }
}