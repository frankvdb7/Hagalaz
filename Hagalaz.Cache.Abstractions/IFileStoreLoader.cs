namespace Hagalaz.Cache.Abstractions
{
    public interface IFileStoreLoader
    {
        IFileStore Open(string rootPath);
    }
}
