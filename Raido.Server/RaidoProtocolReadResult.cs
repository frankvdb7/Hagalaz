namespace Raido.Server
{
    public readonly struct RaidoProtocolReadResult<TMessage>
    {
        public RaidoProtocolReadResult(TMessage? message, bool isCanceled, bool isCompleted)
        {
            Message = message;
            IsCanceled = isCanceled;
            IsCompleted = isCompleted;
        }

        public TMessage? Message { get; }
        public bool IsCanceled { get; }
        public bool IsCompleted { get; }
    }
}