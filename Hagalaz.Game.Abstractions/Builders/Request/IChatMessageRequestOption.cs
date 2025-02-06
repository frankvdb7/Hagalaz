using System;

namespace Hagalaz.Game.Abstractions.Builders.Request
{
    public interface IChatMessageRequestOption
    {
        IChatMessageRequestBuild WithOption(Action<IChatMessageRequestOptionBuilder> optionBuilder);
    }
}