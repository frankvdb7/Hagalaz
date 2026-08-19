using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Factories;

/// <summary>
/// Creates widget scripts in the owning character's scope.
/// </summary>
public interface IWidgetScriptActivator
{
    /// <summary>
    /// Creates a widget script of the requested registered type.
    /// </summary>
    /// <param name="character">The character that owns the scope.</param>
    /// <param name="scriptType">The concrete widget script type.</param>
    /// <returns>The created widget script.</returns>
    IWidgetScript Create(ICharacter character, Type scriptType);
}
