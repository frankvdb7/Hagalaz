using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Abstractions.Types.Providers.Model;

namespace Hagalaz.Cache.Types.Providers
{
    public class HuffmanCodeProvider : IHuffmanCodeProvider
    {
        private readonly ICacheAPI _cacheApi;
        private HuffmanCoding? _coding;

        public HuffmanCodeProvider(ICacheAPI cacheApi) => _cacheApi = cacheApi;

        public HuffmanCoding GetCoding()
        {
            if (_coding != null)
            {
                return _coding;
            }
            var data = _cacheApi.ReadContainer(10, _cacheApi.GetFileId(10, "huffman")).Data;
            var masks = new int[data.Length];
            var bitSizes = data.ToArray();
            var decryptKeys = new int[8];
            var maskData = new int[33];
            var decryptKey = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var bitSize = bitSizes[i];
                if (bitSize == 0)
                {
                    continue;
                }

                var flag = 1 << 32 - bitSize;
                var mask = maskData[bitSize];
                masks[i] = mask;
                int maskOffset;
                if ((mask & flag) != 0)
                {
                    maskOffset = maskData[bitSize - 1];
                }
                else
                {
                    maskOffset = mask | flag;
                    for (var j = bitSize - 1; j >= 1; j--)
                    {
                        var prevMask = maskData[j];
                        if (mask != prevMask)
                        {
                            break;
                        }
                        var prevFlag = 1 << 32 - j;
                        if (0 != (prevMask & prevFlag))
                        {
                            maskData[j] = maskData[j - 1];
                            break;
                        }

                        maskData[j] = prevMask | prevFlag;
                    }
                }
                maskData[bitSize] = maskOffset;

                for (var j = bitSize + 1; j <= 32; j++)
                {
                    if (maskData[j] == mask)
                    {
                        maskData[j] = maskOffset;
                    }
                }

                var decryptKeyIndex = 0;
                for (var j = 0; j < bitSize; j++)
                {
                    var decryptFlag = (int)(unchecked((uint)int.MinValue) >> j);
                    if ((mask & decryptFlag) != 0)
                    {
                        if (decryptKeys[decryptKeyIndex] == 0)
                        {
                            decryptKeys[decryptKeyIndex] = decryptKey;
                        }

                        decryptKeyIndex = decryptKeys[decryptKeyIndex];
                    }
                    else
                    {
                        decryptKeyIndex++;
                    }

                    if (decryptKeyIndex < decryptKeys.Length)
                    {
                        continue;
                    }

                    var decryptKeysCopy = new int[decryptKeys.Length * 2];
                    decryptKeys.CopyTo(decryptKeysCopy, 0);
                    decryptKeys = decryptKeysCopy;
                }

                decryptKeys[decryptKeyIndex] = (int)(i ^ 0xffffffff);
                if (decryptKeyIndex >= decryptKey)
                {
                    decryptKey = 1 + decryptKeyIndex;
                }
            }
            return _coding = new HuffmanCoding(bitSizes, masks, decryptKeys);
        }
    }
}