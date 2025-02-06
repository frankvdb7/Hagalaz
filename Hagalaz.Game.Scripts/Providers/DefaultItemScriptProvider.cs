using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultItemScriptProvider : IDefaultItemScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultItemScript);
    }
}