namespace Raido.Server
{
    /// <summary>
    /// Provides access to the <see cref="RaidoCallerContext"/> for the current invocation.
    /// </summary>
    public interface IRaidoCallerContextAccessor
    {
        /// <summary>
        /// Gets or sets the <see cref="RaidoCallerContext"/> for the current invocation.
        /// </summary>
        RaidoCallerContext Context { get; set; }
    }
}
