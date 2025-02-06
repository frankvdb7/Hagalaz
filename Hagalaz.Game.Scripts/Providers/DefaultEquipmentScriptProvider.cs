using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Providers
{
    public class DefaultEquipmentScriptProvider : IDefaultEquipmentScriptProvider
    {
        public Type GetScriptType() => typeof(DefaultEquipmentScript);
    }
}