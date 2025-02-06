using System;

namespace Raido.Common.Protocol
{
    /// <summary>
    /// Defines message size that are noted by the client.
    /// </summary>
    public enum RaidoMessageSize
    {
        /// <summary>
        /// A fixed size message where the size never changes.
        /// </summary>
        Fixed,
        /// <summary>
        /// A variable message where the size is described by a byte.
        /// </summary>
        VariableByte,
        /// <summary>
        /// A variable message where the size is described by a short.
        /// </summary>
        VariableShort
    }
}
