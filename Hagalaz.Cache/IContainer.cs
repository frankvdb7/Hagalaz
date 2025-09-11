using System;
using System.IO;

namespace Hagalaz.Cache
{
    public interface IContainer : IDisposable
    {
        CompressionType CompressionType { get; }
        MemoryStream Data { get; }
        short Version { get; set; }
        bool IsVersioned();
        void RemoveVersion();
        byte[] Encode(bool writeVersion = true);
    }
}
