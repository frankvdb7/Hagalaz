using System;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Factories;

/// <inheritdoc />
public sealed class WidgetScriptActivator : IWidgetScriptActivator
{
    /// <inheritdoc />
    public IWidgetScript Create(ICharacter character, Type scriptType) =>
        (IWidgetScript)character.ServiceProvider.GetRequiredService(scriptType);
}
