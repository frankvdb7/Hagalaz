using System;

namespace Raido.Common.Protocol
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RaidoMessageHandlerAttribute : Attribute
    {
        public Type Message { get; }

        public RaidoMessageHandlerAttribute(Type message) => Message = message;
    }
}