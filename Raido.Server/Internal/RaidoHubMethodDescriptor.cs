using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Raido.Server.Internal.Reflection;

namespace Raido.Server.Internal
{
    internal sealed class RaidoHubMethodDescriptor
    {
        // bitset to store which parameters come from DI up to 64 arguments
        private ulong _isServiceArgument;

        public ObjectMethodExecutor MethodExecutor { get; }

        public IReadOnlyList<Type> ParameterTypes { get; }

        public Type NonAsyncReturnType { get; }

        public IList<IAuthorizeData> Policies { get; }

        public bool IsServiceArgument(int argumentIndex) => (_isServiceArgument & (1UL << argumentIndex)) != 0;

        public RaidoHubMethodDescriptor(ObjectMethodExecutor methodExecutor, IServiceProviderIsService? serviceProviderIsService, IEnumerable<IAuthorizeData> policies)
        {
            MethodExecutor = methodExecutor;

            NonAsyncReturnType = (MethodExecutor.IsMethodAsync)
                ? MethodExecutor.AsyncResultType
                : MethodExecutor.MethodReturnType;

            // Take out synthetic arguments that will be provided by the server, this list will be given to the protocol parsers
            ParameterTypes = methodExecutor.MethodParameters.Where((p, index) =>
            {
                if (p.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)) || serviceProviderIsService?.IsService(GetServiceType(p.ParameterType)) == true)
                {
                    if (index >= 64)
                    {
                        throw new InvalidOperationException("Hub methods can't use services from DI in the parameters after the 64th parameter.");
                    }
                    _isServiceArgument |= (1UL << index);
                    return false;
                }
                return true;
            }).Select(p => p.ParameterType).ToArray();

            Policies = policies.ToArray();
        }


        private static Type GetServiceType(Type type)
        {
            // IServiceProviderIsService will special case IEnumerable<> and always return true
            // so, in this case checking the element type instead
            if (type.IsConstructedGenericType &&
                type.GetGenericTypeDefinition() is Type genericDefinition &&
                genericDefinition == typeof(IEnumerable<>))
            {
                return type.GenericTypeArguments[0];
            }

            return type;
        }
    }
}