namespace Hagalaz.Cache
{
    public interface ICacheWriter
    {
        void Write(int indexId, int fileId, IContainer container);
    }
}
