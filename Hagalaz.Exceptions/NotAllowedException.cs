namespace Hagalaz.Exceptions
{
    public class NotAllowedException : Exception
    {
        public NotAllowedException() : base() { }

        public NotAllowedException(string message) : base(message) { }
    }
}