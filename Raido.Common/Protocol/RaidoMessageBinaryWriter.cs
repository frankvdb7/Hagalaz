using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raido.Common.Buffers;

namespace Raido.Common.Protocol
{
    public sealed class RaidoMessageBinaryWriter : IRaidoMessageBinaryWriter, IRaidoMessageBitWriter
    {
        private static readonly int[] _bitMasks =
        [
            0, 0x1, 0x3, 0x7,
            0xf, 0x1f, 0x3f, 0x7f,
            0xff, 0x1ff, 0x3ff, 0x7ff,
            0xfff, 0x1fff, 0x3fff, 0x7fff,
            0xffff, 0x1ffff, 0x3ffff, 0x7ffff,
            0xfffff, 0x1fffff, 0x3fffff, 0x7fffff,
            0xffffff, 0x1ffffff, 0x3ffffff, 0x7ffffff,
            0xfffffff, 0x1fffffff, 0x3fffffff, 0x7fffffff,
            -1
        ];

        private readonly IByteBufferWriter _buffer;
        private int _bitPosition;
        private int _initializedBytes;

        public int Opcode { get; private set; }
        public RaidoMessageSize Size { get; private set; }
        public long Length => _buffer.Length;

        public RaidoMessageBinaryWriter(IByteBufferWriter buffer) => _buffer = buffer;

        public IRaidoMessageBinaryWriter SetOpcode(int opcode)
        {
            Opcode = opcode;
            return this;
        }
        public IRaidoMessageBinaryWriter SetSize(RaidoMessageSize size)
        {
            Size = size;
            return this;
        }
        public void Advance(int count) => _buffer.Advance(count);
        public Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);
        public Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);
        public IByteBufferWriter Write(ReadOnlySpan<byte> span) => _buffer.Write(span);
        public IByteBufferWriter WriteByte(byte value) => _buffer.WriteByte(value);
        public IRaidoMessageBitWriter BeginBitAccess()
        {
            _bitPosition = (int)(Length * 8);
            _initializedBytes = (int)Length;
            return this;
        }
        public IRaidoMessageBitWriter WriteBits(int bitCount, int value)
        {
            if (bitCount <= 0 || bitCount > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount), "Number of bits must be between 1 and 32 inclusive");
            }
            var startByte = _bitPosition >> 3;
            var bitOffset = 8 - (_bitPosition & 7);
            _bitPosition += bitCount;

            var endByte = (_bitPosition + 7) >> 3;
            var byteSize = endByte - startByte;
            var bytePosition = 0;
            var writtenSpan = GetSpan(byteSize);

            for (var i = Math.Max(_initializedBytes, startByte); i < endByte; i++)
            {
                writtenSpan[i - startByte] = 0;
            }
            _initializedBytes = Math.Max(_initializedBytes, endByte);
            for (; bitCount > bitOffset; bitOffset = 8)
            {
                writtenSpan[bytePosition] &= (byte)~_bitMasks[bitOffset];
                writtenSpan[bytePosition++] |= (byte)((value >> (bitCount - bitOffset)) & _bitMasks[bitOffset]);
                bitCount -= bitOffset;
            }
            if (bitCount == bitOffset)
            {
                writtenSpan[bytePosition] &= (byte)~_bitMasks[bitOffset];
                writtenSpan[bytePosition] |= (byte)(value & _bitMasks[bitOffset]);
                bytePosition++;
            }
            else
            {
                writtenSpan[bytePosition] &= (byte)~(_bitMasks[bitCount] << (bitOffset - bitCount));
                writtenSpan[bytePosition] |= (byte)((value & _bitMasks[bitCount]) << (bitOffset - bitCount));
            }
            Advance(bytePosition);
            return this;
        }
        public IRaidoMessageBinaryWriter EndBitAccess()
        {
            var calcBytePosition = (_bitPosition + 7) / 8;
            Advance((int)(calcBytePosition - Length));
            _initializedBytes = (int)Length;
            return this;
        }
    }
}
