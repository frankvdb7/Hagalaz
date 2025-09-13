namespace Hagalaz.Cache
{
    public interface IArchiveDecoder
    {
        Archive Decode(IContainer container, int size);
    }
}
