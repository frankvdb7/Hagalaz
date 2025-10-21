using System;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// An attribute used to mark a method as a Raido message handler.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RaidoMessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// Gets the type of the message that the handler can process.
        /// </summary>
        public Type Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoMessageHandlerAttribute"/> class.
        /// </summary>
        /// <param name="message">The type of the message that the handler can process.</param>
        public RaidoMessageHandlerAttribute(Type message) => Message = message;
    }
}