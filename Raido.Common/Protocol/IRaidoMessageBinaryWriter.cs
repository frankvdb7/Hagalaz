using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raido.Common.Buffers;

namespace Raido.Common.Protocol
{
    public interface IRaidoMessageBinaryWriter : IByteBufferWriter, IRaidoMessageBitWriterBegin
    {
        IRaidoMessageBinaryWriter SetOpcode(int opcode);
        IRaidoMessageBinaryWriter SetSize(RaidoMessageSize size);
    }
}
