using System;
using System.IO;
using System.Threading.Tasks;
using Hagalaz.Cache;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameUpdate.Services
{
    public class CacheService : ICacheService
    {
        private readonly ICacheAPI _cacheApi;
        private readonly IOptions<RsaConfig> _rsaOptions;
        private readonly IChecksumTableCodec _checksumTableCodec;
        private readonly Lazy<byte[]> _encodedChecksumTable;

        public CacheService(ICacheAPI cacheApi, IOptions<RsaConfig> rsaOptions, IChecksumTableCodec checksumTableCodec)
        {
            _cacheApi = cacheApi;
            _rsaOptions = rsaOptions;
            _checksumTableCodec = checksumTableCodec;
            _encodedChecksumTable = new Lazy<byte[]>(EncodeChecksumTable);
        }

        private byte[] EncodeChecksumTable()
        {
            using (var table = _cacheApi.CreateChecksumTable())
            {
                var rsa = _rsaOptions.Value;
                var encodedTable = _checksumTableCodec.Encode(table, true, rsa.ModulusKey, rsa.PrivateKey);
                using (var container = new Container(encodedTable))
                {
                    return container.Encode();
                }
            }
        }

        public ValueTask<MemoryStream> ReadFileAsync(byte indexId, int fileId)
        {
            if (indexId == 255 && fileId == 255)
            {
                return new ValueTask<MemoryStream>(new MemoryStream(_encodedChecksumTable.Value));
            }
            return ValueTask.FromResult<MemoryStream>(_cacheApi.Read(indexId, fileId));
        }

        public bool IsValidFile(byte indexId, int fileId)
        {
            if (indexId == 255 && fileId == 255) // ukeys
                return true;
            if (indexId >= _cacheApi.GetFileCount(255) && indexId != 255)
                return false;
            if (fileId < 0 || fileId >= _cacheApi.GetFileCount(indexId))
                return false;
            return true;
        }
    }
}