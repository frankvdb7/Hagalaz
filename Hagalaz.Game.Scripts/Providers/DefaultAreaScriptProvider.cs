using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Maps;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultAreaScriptProvider : IDefaultAreaScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultAreaScript);
    }
}