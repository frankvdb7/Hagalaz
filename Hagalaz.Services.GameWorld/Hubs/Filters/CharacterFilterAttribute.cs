using System;

namespace Hagalaz.Services.GameWorld.Hubs.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CharacterFilterAttribute : Attribute
    {
    }
}
