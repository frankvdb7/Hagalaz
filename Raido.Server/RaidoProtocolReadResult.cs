namespace Raido.Server
{
    /// <summary>
    /// Represents the result of a protocol read operation.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public readonly struct RaidoProtocolReadResult<TMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoProtocolReadResult{TMessage}"/> struct.
        /// </summary>
        /// <param name="message">The message that was read.</param>
        /// <param name="isCanceled">A value indicating whether the read operation was canceled.</param>
        /// <param name="isCompleted">A value indicating whether the read operation is complete.</param>
        public RaidoProtocolReadResult(TMessage? message, bool isCanceled, bool isCompleted)
        {
            Message = message;
            IsCanceled = isCanceled;
            IsCompleted = isCompleted;
        }

        /// <summary>
        /// Gets the message that was read.
        /// </summary>
        public TMessage? Message { get; }

        /// <summary>
        /// Gets a value indicating whether the read operation was canceled.
        /// </summary>
        public bool IsCanceled { get; }

        /// <summary>
        /// Gets a value indicating whether the read operation is complete.
        /// </summary>
        public bool IsCompleted { get; }
    }
}