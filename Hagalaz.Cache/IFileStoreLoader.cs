namespace Hagalaz.Cache
{
    public interface IFileStoreLoader
    {
        IFileStore Open(string rootPath);
    }
}
