using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Raido.Server.Internal.Reflection
{
    internal static class RaidoHubReflectionHelper
    {
        private static readonly Type[] _excludeInterfaces = { typeof(IDisposable) };

        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
        {
            var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

            return methods.Except(allInterfaceMethods).Where(IsHubMethod);
        }

        private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface) => !iface.IsAssignableFrom(type) ? Enumerable.Empty<MethodInfo>() : type.GetInterfaceMap(iface).TargetMethods;

        private static bool IsHubMethod(MethodInfo methodInfo)
        {
            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType!;
            if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseType = baseDefinition.IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            return typeof(RaidoHub) != baseType;
        }
    }
}