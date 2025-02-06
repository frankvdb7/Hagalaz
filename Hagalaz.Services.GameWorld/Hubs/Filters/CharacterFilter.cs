using System.Threading.Tasks;
using System;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;
using System.Reflection;

namespace Hagalaz.Services.GameWorld.Hubs.Filters
{
    public class CharacterFilter : IRaidoHubFilter
    {
        private readonly Type _attributeType;

        public CharacterFilter() => _attributeType = typeof(CharacterFilterAttribute);

        public ValueTask<object?> InvokeMethodAsync(RaidoHubInvocationContext invocationContext, Func<RaidoHubInvocationContext, ValueTask<object?>> next)
        {
            var hubType = invocationContext.Hub.GetType();
            var characterFilter = hubType.GetCustomAttribute(_attributeType, inherit: true);
            characterFilter ??= invocationContext.HubMethod.GetCustomAttribute(_attributeType, inherit: true);
            if (characterFilter != null)
            {
                if (invocationContext.Context.GetCharacter() == null)
                {
                    return ValueTask.FromResult<object?>(null);
                }
            }
            return next(invocationContext);
        }
    }
}
