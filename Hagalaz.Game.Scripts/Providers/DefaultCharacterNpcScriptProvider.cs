using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultCharacterNpcScriptProvider : IDefaultCharacterNpcScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultCharacterNpcScript);
    }
}