namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// Utilities , for decrypting xtea.
    /// </summary>
    public static class XteaDecryptor
    {

        /// <summary>
        /// Decrypts xtea with given parameters.
        /// </summary>
        /// <param name="keys">XTEA Keys.</param>
        /// <param name="data">Data to decrypt.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <param name="length">Length of the data in buffer to be decrypted.</param>
        /// <returns>Decrypted data.</returns>
        public static byte[] DecryptXtea(int[] keys, byte[] data, int offset, int length)
        {
            int encLength = (length - offset) / 8; // first off , we calculate how much data we can decrypt
                                                   // this is simple cause decryption reads 2 ints and then writes those 2 decrypted
                                                   // so minimum size of the data must be 8 bytes.
            var x = new XteaStream(data)
            {
                Position = offset
            };
            for (int i = 0; i < encLength; i++)
            {
                int v0 = x.ReadInt();
                int v1 = x.ReadInt();
                int sum = -957401312;
                int delta = -1640531527;
                int numRounds = 32; // Rounds to encrypt with , A related-key differential attack can break 27 out of 64 rounds of XTEA , however jagex uses 32 :(.
                while (numRounds-- > 0)
                {
                    v1 -= (((int)((uint)v0 >> -1563092443) ^ v0 << 611091524) + v0 ^ sum + keys[(int)((uint)sum >> -1002502837) & 0x56c00003]);
                    sum -= delta;
                    v0 -= (((int)((uint)v1 >> 1337206757) ^ v1 << 363118692) - -v1 ^ sum + keys[sum & 0x3]);
                }
                x.Position -= 8;
                x.WriteInt(v0);
                x.WriteInt(v1);
            }
            return x.Buffer;
        }

        /// <summary>
        /// Contains methods for writing specific type data.
        /// </summary>
        private class XteaStream
        {
            public readonly byte[] Buffer;
            public int Position;

            /// <summary>
            /// Constructs new xtea stream.
            /// </summary>
            /// <param name="data"></param>
            public XteaStream(byte[] data)
            {
                Buffer = data;
                Position = 0;
            }

            ///// <summary>
            /// <summary>
            /// Writes the int.
            /// </summary>
            /// <param name="data">The data.</param>
            public void WriteInt(int data)
            {
                Buffer[Position++] = (byte)(data >> 24);
                Buffer[Position++] = (byte)(data >> 16);
                Buffer[Position++] = (byte)(data >> 8);
                Buffer[Position++] = (byte)data;
            }

            /// <summary>
            /// Read's int from buffer.
            /// </summary>
            /// <returns></returns>
            public int ReadInt()
            {
                Position += 4;
                return ((Buffer[Position - 1] & 0xff) 
                    + ((Buffer[Position - 3] << 16 & 0xff0000) 
                    + (((Buffer[Position - 4] & 0xff) << 24) 
                    + (Buffer[Position - 2] << 8 & 0xff00))));
            }
        }
    }
}
