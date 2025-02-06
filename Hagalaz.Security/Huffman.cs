using System;
using System.IO;
using System.Text;

namespace Hagalaz.Security
{
    public static class Huffman
    {
        private static readonly int[] _huffmanDecryptKeys =
        {
            215, 203, 83, 158, 104, 101, 93, 84,
            107, 103, 109, 95, 94, 98, 89, 86, 70, 41, 32, 27, 24, 23, -1, -2,
            26, -3, -4, 31, 30, -5, -6, -7, 37, 38, 36, -8, -9, -10, 40, -11,
            -12, 55, 48, 46, 47, -13, -14, -15, 52, 51, -16, -17, 54, -18, -19,
            63, 60, 59, -20, -21, 62, -22, -23, 67, 66, -24, -25, 69, -26, -27,
            199, 132, 80, 77, 76, -28, -29, 79, -30, -31, 87, 85, -32, -33,
            -34, -35, -36, 197, -37, 91, -38, 134, -39, -40, -41, 97, -42, -43,
            133, 106, -44, 117, -45, -46, 139, -47, -48, 110, -49, -50, 114,
            113, -51, -52, 116, -53, -54, 135, 138, 136, 129, 125, 124, -55,
            -56, 130, 128, -57, -58, -59, 183, -60, -61, -62, -63, -64, 148,
            -65, -66, 153, 149, 145, 144, -67, -68, 147, -69, -70, -71, 152,
            154, -72, -73, -74, 157, 171, -75, -76, 207, 184, 174, 167, 166,
            165, -77, -78, -79, 172, 170, -80, -81, -82, 178, -83, 177, 182,
            -84, -85, 187, 181, -86, -87, -88, -89, 206, 221, -90, 189, -91,
            198, 254, 262, 195, 196, -92, -93, -94, -95, -96, 252, 255, 250,
            -97, 211, 209, -98, -99, 212, -100, 213, -101, -102, -103, 224,
            -104, 232, 227, 220, 226, -105, -106, 246, 236, -107, 243, -108,
            -109, 231, 237, 235, -110, -111, 239, 238, -112, -113, -114, -115,
            -116, 241, -117, 244, -118, -119, 248, -120, 249, -121, -122, -123,
            253, -124, -125, -126, -127, 259, 258, -128, -129, 261, -130, -131,
            390, 327, 296, 281, 274, 271, 270, -132, -133, 273, -134, -135,
            278, 277, -136, -137, 280, -138, -139, 289, 286, 285, -140, -141,
            288, -142, -143, 293, 292, -144, -145, 295, -146, -147, 312, 305,
            302, 301, -148, -149, 304, -150, -151, 309, 308, -152, -153, 311,
            -154, -155, 320, 317, 316, -156, -157, 319, -158, -159, 324, 323,
            -160, -161, 326, -162, -163, 359, 344, 337, 334, 333, -164, -165,
            336, -166, -167, 341, 340, -168, -169, 343, -170, -171, 352, 349,
            348, -172, -173, 351, -174, -175, 356, 355, -176, -177, 358, -178,
            -179, 375, 368, 365, 364, -180, -181, 367, -182, -183, 372, 371,
            -184, -185, 374, -186, -187, 383, 380, 379, -188, -189, 382, -190,
            -191, 387, 386, -192, -193, 389, -194, -195, 454, 423, 408, 401,
            398, 397, -196, -197, 400, -198, -199, 405, 404, -200, -201, 407,
            -202, -203, 416, 413, 412, -204, -205, 415, -206, -207, 420, 419,
            -208, -209, 422, -210, -211, 439, 432, 429, 428, -212, -213, 431,
            -214, -215, 436, 435, -216, -217, 438, -218, -219, 447, 444, 443,
            -220, -221, 446, -222, -223, 451, 450, -224, -225, 453, -226, -227,
            486, 471, 464, 461, 460, -228, -229, 463, -230, -231, 468, 467,
            -232, -233, 470, -234, -235, 479, 476, 475, -236, -237, 478, -238,
            -239, 483, 482, -240, -241, 485, -242, -243, 499, 495, 492, 491,
            -244, -245, 494, -246, -247, 497, -248, 502, -249, 506, 503, -250,
            -251, 505, -252, -253, 508, -254, 510, -255, -256, 0
        };
        
        private static readonly int[] _huffmanMasks =
        {
            0, 1024, 2048, 3072, 4096, 5120,
            6144, 8192, 9216, 12288, 10240, 11264, 16384, 18432, 17408, 20480,
            21504, 22528, 23552, 24576, 25600, 26624, 27648, 28672, 29696,
            30720, 31744, 32768, 33792, 34816, 35840, 36864, 536870912,
            16777216, 37888, 65536, 38912, 131072, 196608, 33554432, 524288,
            1048576, 1572864, 262144, 67108864, 4194304, 134217728, 327680,
            8388608, 2097152, 12582912, 13631488, 14680064, 15728640,
            100663296, 101187584, 101711872, 101974016, 102760448, 102236160,
            40960, 393216, 229376, 117440512, 104857600, 109051904, 201326592,
            205520896, 209715200, 213909504, 106954752, 218103808, 226492416,
            234881024, 222298112, 224395264, 268435456, 272629760, 276824064,
            285212672, 289406976, 223346688, 293601280, 301989888, 318767104,
            297795584, 298844160, 310378496, 102498304, 335544320, 299892736,
            300941312, 301006848, 300974080, 39936, 301465600, 49152,
            1073741824, 369098752, 402653184, 1342177280, 1610612736,
            469762048, 1476395008, -2147483648, -1879048192, 352321536,
            1543503872, -2013265920, -1610612736, -1342177280, -1073741824,
            -1543503872, 356515840, -1476395008, -805306368, -536870912,
            -268435456, 1577058304, -134217728, 360710144, -67108864,
            364904448, 51200, 57344, 52224, 301203456, 53248, 54272, 55296,
            56320, 301072384, 301073408, 301074432, 301075456, 301076480,
            301077504, 301078528, 301079552, 301080576, 301081600, 301082624,
            301083648, 301084672, 301085696, 301086720, 301087744, 301088768,
            301089792, 301090816, 301091840, 301092864, 301093888, 301094912,
            301095936, 301096960, 301097984, 301099008, 301100032, 301101056,
            301102080, 301103104, 301104128, 301105152, 301106176, 301107200,
            301108224, 301109248, 301110272, 301111296, 301112320, 301113344,
            301114368, 301115392, 301116416, 301117440, 301118464, 301119488,
            301120512, 301121536, 301122560, 301123584, 301124608, 301125632,
            301126656, 301127680, 301128704, 301129728, 301130752, 301131776,
            301132800, 301133824, 301134848, 301135872, 301136896, 301137920,
            301138944, 301139968, 301140992, 301142016, 301143040, 301144064,
            301145088, 301146112, 301147136, 301148160, 301149184, 301150208,
            301151232, 301152256, 301153280, 301154304, 301155328, 301156352,
            301157376, 301158400, 301159424, 301160448, 301161472, 301162496,
            301163520, 301164544, 301165568, 301166592, 301167616, 301168640,
            301169664, 301170688, 301171712, 301172736, 301173760, 301174784,
            301175808, 301176832, 301177856, 301178880, 301179904, 301180928,
            301181952, 301182976, 301184000, 301185024, 301186048, 301187072,
            301188096, 301189120, 301190144, 301191168, 301193216, 301195264,
            301194240, 301197312, 301198336, 301199360, 301201408, 301202432
        };
        
        private static readonly byte[] _huffmanBitSizes =
        {
            22, 22, 22, 22, 22, 22, 21, 22, 22,
            20, 22, 22, 22, 21, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 3, 8, 22, 16, 22, 16, 17, 7, 13, 13, 13,
            16, 7, 10, 6, 16, 10, 11, 12, 12, 12, 12, 13, 13, 14, 14, 11, 14,
            19, 15, 17, 8, 11, 9, 10, 10, 10, 10, 11, 10, 9, 7, 12, 11, 10, 10,
            9, 10, 10, 12, 10, 9, 8, 12, 12, 9, 14, 8, 12, 17, 16, 17, 22, 13,
            21, 4, 7, 6, 5, 3, 6, 6, 5, 4, 10, 7, 5, 6, 4, 4, 6, 10, 5, 4, 4,
            5, 7, 6, 10, 6, 10, 22, 19, 22, 14, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 21, 22, 21, 22, 22, 22, 21,
            22, 22
        };

        /*public static void Intialize(Cache cache)
        {
            /*CacheFile file = cache.GetFile(10, cache.GetFileInformation(10, cache.GetFileID(10, "huffman")).Index);
            if (file == null)
                return;

            huffmanBitSizes = file.Decompress();
            huffmanMasks = new int[huffmanBitSizes.Length];
            //huffmanDecryptKeys = new int[8];
            int[] huffmanMaskData = new int[33];

            //int currentDecryptKey = 0;
            for (int i = 0; i < huffmanBitSizes.Length; i++)
            {
                int bitSize = huffmanBitSizes[i];
                if (bitSize != 0)
                {
                    int huffmanFlag = 1 << 32 - bitSize;
                    int huffmanMask = huffmanMaskData[bitSize];
                    huffmanMasks[i] = huffmanMask;
                    int i_6_;
                    if ((huffmanMask & huffmanFlag) != 0)
                        i_6_ = huffmanMaskData[bitSize - 1];
                    else
                    {
                        for (int i_7_ = bitSize - 1; i_7_ >= 1; i_7_--)
                        {
                            int previousHuffmanMask = huffmanMaskData[i_7_];
                            if (huffmanMask != previousHuffmanMask)
                                break;
                            int previousHuffmanFlag = 1 << 32 - i_7_;
                            if ((previousHuffmanMask & previousHuffmanFlag) != 0)
                            {
                                huffmanMaskData[i_7_] = huffmanMaskData[i_7_ - 1];
                                break;
                            }
                            huffmanMaskData[i_7_] = previousHuffmanMask | previousHuffmanFlag;
                        }
                        i_6_ = huffmanMask | huffmanFlag;
                    }
                    huffmanMaskData[bitSize] = i_6_;
                    for (int i_10_ = bitSize + 1; i_10_ <= 32; i_10_++)
                        if (huffmanMaskData[i_10_] == huffmanMask)
                            huffmanMaskData[i_10_] = i_6_;*/
        /* TODO fix this
        int currentDecryptKeyIndex = 0;
        for (int i_12_ = 0; i_12_ < bitSize; i_12_++)
        {
            int flag = (int)(uint)(-2147483648 >> i_12_);
            if ((huffmanMask & flag) != 0)
            {
                if (huffmanDecryptKeys[currentDecryptKeyIndex] == 0)
                    huffmanDecryptKeys[currentDecryptKeyIndex] = currentDecryptKey;
                currentDecryptKeyIndex = huffmanDecryptKeys[currentDecryptKeyIndex];
            }
            else
                currentDecryptKeyIndex++;
            if (currentDecryptKeyIndex >= huffmanDecryptKeys.Length)
            {
                int[] newHuffmanDecryptKeys = new int[huffmanDecryptKeys.Length * 2];
                for (int i_15_ = 0; i_15_ < huffmanDecryptKeys.Length; i_15_++)
                    newHuffmanDecryptKeys[i_15_] = huffmanDecryptKeys[i_15_];
                huffmanDecryptKeys = newHuffmanDecryptKeys;
            }
            flag = (int)((uint)flag >> 1);
        }

        huffmanDecryptKeys[currentDecryptKeyIndex] = i;
        if (currentDecryptKeyIndex >= currentDecryptKey)
            currentDecryptKey = 1 + currentDecryptKeyIndex;*/
        /*}
    }
    }*/

        /// <summary>
        /// Decrpyts a character's chat.
        /// </summary>
        /// <param name="stream">The packet holding the bytes.</param>
        /// <param name="length">The length of the characters.</param>
        /// <returns>Returns a string representing the text that the character has typed out.</returns>
        public static string Decode(MemoryStream stream, int length)
        {
            try
            {
                if (length == 0)
                    return string.Empty;
                var totalChars = length;
                var charsDecoded = 0;
                var keyIndex = 0;
                var value = string.Empty;
                for (; ; )
                {
                    //sbyte i_7_ = unchecked((sbyte)p.ReadByte());
                    var val = (sbyte)stream.ReadByte();
                    if (val >= 0)
                    {
                        keyIndex++;
                    }
                    else
                    {
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    int keyValue;
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (length <= ++charsDecoded)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x40) ^ 0xffffffff) != -1)
                    {
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (++charsDecoded >= length)
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
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (length <= ++charsDecoded)
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
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (length <= ++charsDecoded)
                        {
                            break;
                        }

                        keyIndex = 0;
                    }
                    if ((int)((0x8 & val) ^ 0xffffffff) != -1)
                    {
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (++charsDecoded >= length)
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
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (length <= ++charsDecoded)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x2) ^ 0xffffffff) != -1)
                    {
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (length <= ++charsDecoded)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                    if ((int)((val & 0x1) ^ 0xffffffff) != -1)
                    {
                        keyIndex = _huffmanDecryptKeys[keyIndex];
                    }
                    else
                    {
                        keyIndex++;
                    }
                    if ((keyValue = _huffmanDecryptKeys[keyIndex]) < 0)
                    {
                        value += (char)(byte)(keyValue ^ 0xffffffff);
                        if (++charsDecoded >= length)
                        {
                            break;
                        }
                        keyIndex = 0;
                    }
                }
                return value;
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }

        /// <summary>
        /// Encodes the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="messageLength">Length of the message.</param>
        /// <returns></returns>
        public static byte[] Encode(string text, out int messageLength)
        {
            var messageData = Encoding.ASCII.GetBytes(text);
            var length = messageData.Length;
            if (length > short.MaxValue)
                length = short.MaxValue;
            messageLength = length;
            var encodingBuffer = new byte[length * 2];
            var actualLength = Huffman.Encode(encodingBuffer, 0, 0, length, messageData);
            var encoded = new byte[actualLength];
            Array.Copy(encodingBuffer, encoded, actualLength);
            return encoded;
        }

        /// <summary>
        /// Encrypts a character's chat.
        /// </summary>
        /// <param name="encodedText">The chat to encrpyt.</param>
        /// <param name="textOffset">Unknown.</param>
        /// <param name="streamOffset">Unknown.</param>
        /// <param name="textLength">Unknown.</param>
        /// <param name="text">Unknown.</param>
        /// <returns>Returns an int representing the encrpyted value.</returns>
        public static int Encode(byte[] encodedText, int textOffset, int streamOffset, int textLength, byte[] text)
        {

            var key = 0;
            var position = streamOffset << 3;
            for (textLength += textOffset; textOffset < textLength; textOffset++)
            {
                var character = 0xff & text[textOffset];
                var mask = _huffmanMasks[character];
                int size = _huffmanBitSizes[character];
                if (size == 0)
                    throw new Exception("No codeword for data value " + character);
                var offset = position >> 3;
                var bitoffset = position & 0x7;
                key &= -bitoffset >> 31;
                var i11 = offset + (bitoffset + size - 1 >> 3);
                bitoffset += 24;
                encodedText[offset] = (byte)(key = (key | (int)((uint)mask >> bitoffset)));
                if (offset < i11)
                {
                    offset++;
                    bitoffset -= 8;
                    encodedText[offset] = (byte)(key = (int)((uint)mask >> bitoffset));
                    if (i11 > offset)
                    {
                        offset++;
                        bitoffset -= 8;
                        encodedText[offset] = (byte)(key = (int)((uint)mask >> bitoffset));
                        if (i11 > offset)
                        {
                            bitoffset -= 8;
                            offset++;
                            encodedText[offset] = (byte)(key = (int)((uint)mask >> bitoffset));
                            if (offset < i11)
                            {
                                bitoffset -= 8;
                                offset++;
                                encodedText[offset] = (byte)(key = (int)((uint)mask << -bitoffset));
                            }
                        }
                    }
                }
                position += size;
            }
            return (7 + position >> 3) - streamOffset;
        }
    }
}
