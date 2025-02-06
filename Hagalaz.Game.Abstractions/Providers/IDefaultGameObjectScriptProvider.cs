using System;

namespace Hagalaz.Game.Abstractions.Providers
{
    public interface IDefaultGameObjectScriptProvider
    {
        Type GetScriptType();
    }
}