using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// Activates NPC scripts inside the current NPC scope.
    /// </summary>
    public sealed class NpcScriptActivator(IServiceProvider serviceProvider) : INpcScriptActivator
    {
        public INpcScript Create(Type scriptType, INpc owner, params object[] arguments)
        {
            var constructorArguments = new object[arguments.Length + 1];
            constructorArguments[0] = owner;
            arguments.CopyTo(constructorArguments, 1);
            return (INpcScript)ActivatorUtilities.CreateInstance(serviceProvider, scriptType, constructorArguments);
        }
    }
}
