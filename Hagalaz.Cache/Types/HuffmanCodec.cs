using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    public class HuffmanCodec : IHuffmanDecoder, IHuffmanEncoder
    {
        private readonly IHuffmanCodeProvider _huffmanCodeProvider;
        
        public HuffmanCodec(IHuffmanCodeProvider huffmanCodeProvider) => _huffmanCodeProvider = huffmanCodeProvider;

        public void Encode(string text, IBufferWriter<byte> output)
        {
            var coding = _huffmanCodeProvider.GetCoding();
            var textData = Encoding.ASCII.GetBytes(text);
            var textLength = text.Length;
            var key = 0;
            var position = 0;
            var encodedText = ArrayPool<byte>.Shared.Rent(textLength);
            try
            {
                for (var index = 0; index < textLength; index++)
                {
                    var character = textData[index] & 0xFF;
                    var bitSize = coding.BitSizes[character];
                    if (bitSize == 0)
                    {
                        throw new InvalidOperationException("No codeword for data value " + (char)character);
                    }
                    var mask = coding.Masks[character];
                    var offset = position >> 3;
                    var bitOffset = position & 0x7;
                    key &= -bitOffset >> 31;
                    var byteOffset = offset + (bitOffset + bitSize - 1 >> 3);
                    bitOffset += 24;
                    encodedText[offset] = (byte)(key = (key | (int)((uint)mask >> bitOffset)));
                    if (offset < byteOffset)
                    {
                        offset++;
                        bitOffset -= 8;
                        encodedText[offset] = (byte)(key = (int)((uint)mask >> bitOffset));
                        if (byteOffset > offset)
                        {
                            offset++;
                            bitOffset -= 8;
                            encodedText[offset] = (byte)(key = (int)((uint)mask >> bitOffset));
                            if (byteOffset > offset)
                            {
                                bitOffset -= 8;
                                offset++;
                                encodedText[offset] = (byte)(key = (int)((uint)mask >> bitOffset));
                                if (offset < byteOffset)
                                {
                                    bitOffset -= 8;
                                    offset++;
                                    encodedText[offset] = (byte)(key = (int)((uint)mask << -bitOffset));
                                }
                            }
                        }
                    }
                    position += bitSize;
                }
                var byteCount = 7 + position >> 3;
                output.Write(encodedText.AsSpan()[..byteCount]);
                output.Advance(byteCount);
            } 
            finally
            {
                ArrayPool<byte>.Shared.Return(encodedText);
            }
        }

        public bool TryDecode(in ReadOnlySequence<byte> input, int length, [NotNullWhen(true)] out string? value)
        {
            if (input.Length <= 0 || length <= 0)
            {
                value = string.Empty;
                return true;
            }
            var coding = _huffmanCodeProvider.GetCoding();
            var reader = new SequenceReader<byte>(input);
            var keyIndex = 0;
            var outputIndex = 0;
            var output = ArrayPool<char>.Shared.Rent(length);
            try
            {
                for (;;)
                {
                    if (!reader.TryRead(out byte v))
                    {
                        value = default;
                        return false;
                    }

                    var val = (sbyte)v;
                    if (val >= 0)
                    {
                        keyIndex++;
                    }
                    else
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    int keyValue;
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x40) ^ 0xffffffff) != -1)
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((0x20 & val) == 0)
                    {
                        keyIndex++;
                    }
                    else
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((0x10 & val) ^ 0xffffffff) == -1)
                    {
                        keyIndex++;
                    }
                    else
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((0x8 & val) ^ 0xffffffff) != -1)
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((0x4 & val) == 0)
                    {
                        keyIndex++;
                    }
                    else
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x2) ^ 0xffffffff) != -1)
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x1) ^ 0xffffffff) != -1)
                    {
                        keyIndex = coding.DecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = coding.DecryptKeys[keyIndex]) < 0)
                    {
                        output[outputIndex++] = (char)(byte)(keyValue ^ 0xffffffff);
                        if (outputIndex >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                }
                value = new string(output[..outputIndex]);
                return true;
            } 
            finally
            {
                ArrayPool<char>.Shared.Return(output);
            }
        }
    }
}