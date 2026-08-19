using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories
{
    /// <summary>
    /// Activates familiar scripts from the current character scope.
    /// </summary>
    public sealed class FamiliarScriptActivator : IFamiliarScriptActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public FamiliarScriptActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFamiliarScript Create(Type scriptType) =>
            (IFamiliarScript)_serviceProvider.GetRequiredService(scriptType);
    }
}
