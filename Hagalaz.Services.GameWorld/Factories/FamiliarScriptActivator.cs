using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// Activates familiar scripts from the character scope.
    /// </summary>
    public sealed class FamiliarScriptActivator(IServiceProvider serviceProvider) : IFamiliarScriptActivator
    {
        public IFamiliarScript Create(Type scriptType) =>
            (IFamiliarScript)serviceProvider.GetRequiredService(scriptType);
    }
}
