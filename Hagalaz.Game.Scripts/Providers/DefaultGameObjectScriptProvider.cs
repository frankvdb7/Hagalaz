using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultGameObjectScriptProvider : IDefaultGameObjectScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultGameObjectScript);
    }
}