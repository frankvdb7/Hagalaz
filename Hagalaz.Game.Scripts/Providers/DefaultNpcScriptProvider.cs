using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultNpcScriptProvider : IDefaultNpcScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultNpcScript);
    }
}