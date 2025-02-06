using System;

namespace Hagalaz.Cache
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum ReferenceTableFlags : byte
    {
        /// <summary>
        /// A flag which indicates this table contains
        /// hashed identifiers.
        /// </summary>
        Identifiers = 0x1,
        /// <summary>
        /// A flag which indicates this table contains
	    /// whirlpool digests for its entries.
        /// </summary>
        Digests = 0x2,
    }
}
