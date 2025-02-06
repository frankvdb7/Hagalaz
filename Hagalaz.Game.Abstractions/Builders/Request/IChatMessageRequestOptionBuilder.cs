using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    public interface IChatMessageRequestOptionBuilder
    {
        IChatMessageRequestOptionBuilder WithType(CharacterClickType clickType);
        IChatMessageRequestOptionBuilder WithAction(Action action);
        IChatMessageRequestOptionBuilder WithName(string name);
    }
}