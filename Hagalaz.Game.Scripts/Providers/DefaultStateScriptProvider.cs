using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultStateScriptProvider : IDefaultStateScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultStateScript);
    }
}
