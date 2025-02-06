using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raido.Common.Buffers
{
    public interface IByteBufferWriter : IBufferWriter<byte>
    {
        public long Length { get; }
        public IByteBufferWriter Write(ReadOnlySpan<byte> span);
        public IByteBufferWriter WriteByte(byte value);
    }
}
