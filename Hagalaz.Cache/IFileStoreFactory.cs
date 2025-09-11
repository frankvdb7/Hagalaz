namespace Hagalaz.Cache
{
    public interface IFileStoreFactory
    {
        IFileStore Open(string rootPath);
    }
}
