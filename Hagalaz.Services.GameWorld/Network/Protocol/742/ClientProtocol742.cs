using System;
using System.Buffers;
using System.Linq;
using Hagalaz.Security;
using Raido.Common.Buffers;
using Raido.Common.Protocol;
using RaidoMessageSize = Raido.Common.Protocol.RaidoMessageSize;

namespace Hagalaz.Services.GameWorld.Network.Protocol._742;

public class ClientProtocol742 : IClientProtocol
{
    private readonly IRaidoCodec<ClientProtocol742> _codec;
    private ISAAC _isaacIncoming;
    private ISAAC _isaacOutgoing;

    private const int _defaultLength = -3;
    private static readonly int[] _payLoadSizes = new int[255];

    public ClientProtocol742(IRaidoCodec<ClientProtocol742> codec)
    {
        _codec = codec;
        _isaacIncoming = new ISAAC([]);
        _isaacOutgoing = new ISAAC([]);
    }

    public string Name { get; } = "ClientProtocol742";
    public int Version { get; } = 742;
    public bool IsVersionSupported(int version) => version == Version;
    public void SetEncryptionSeed(uint[] keys)
    {
        _isaacIncoming = new ISAAC(keys);
        _isaacOutgoing = new ISAAC(keys.Select(key => key + 50).ToArray());
    }

    public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output) => TryWriteMessageToBuffer(message, output);

    public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
    {
        var buffer = MemoryBufferWriter.Get();
        try
        {
            if (!TryWriteMessageToBuffer(message, buffer))
            {
                return ReadOnlyMemory<byte>.Empty;
            }
            return buffer.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(buffer);
        }
    }

    public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage? message)
    {
        var reader = new SequenceReader<byte>(input);
        if (!reader.TryRead(out byte encodedOpcode))
        {
            message = default;
            return false;
        }
        byte opcode = (byte)((encodedOpcode - _isaacIncoming.PeekKey()) & 0xFF);
        if (opcode > _payLoadSizes.Length)
        {
            message = default;
            return false;
        }
        var payLoadSize = _payLoadSizes[opcode];
        switch (payLoadSize)
        {
            case -1:
                {
                    if (!reader.TryRead(out byte size))
                    {
                        message = default;
                        return false;
                    }
                    payLoadSize = size;
                    break;
                }
            case -2:
                {
                    if (!reader.TryReadBigEndian(out short size))
                    {
                        message = default;
                        return false;
                    }
                    payLoadSize = size;
                    break;
                }
        }
        if (payLoadSize < 0 || payLoadSize > reader.Remaining)
        {
            message = default;
            return false;
        }

        _isaacIncoming.Next();

        var payload = input.Slice(reader.Position, payLoadSize);
        consumed = payload.End;
        examined = consumed;
        return _codec.TryDecodeMessage(opcode, in payload, out message);
    }

    private bool TryWriteMessageToBuffer(RaidoMessage message, IBufferWriter<byte> output)
    {
        var buffer = MemoryBufferWriter.Get();
        var writer = new RaidoMessageBinaryWriter(buffer);
        var headerBuffer = ArrayPool<byte>.Shared.Rent(4);
        var headerPosition = 0;
        try
        {
            if (!_codec.TryEncodeMessage(message, writer))
            {
                return false;
            }
            if (writer.Opcode >= 128)
            {
                headerBuffer[headerPosition++] = (byte)(128 + _isaacOutgoing.ReadKey());
            }
            headerBuffer[headerPosition++] = (byte)(writer.Opcode + _isaacOutgoing.ReadKey());
            switch (writer.Size)
            {
                // A 8-bit size packet type.
                case RaidoMessageSize.VariableByte when buffer.Length > byte.MaxValue:
                    throw new InvalidOperationException("Could not send message with " + buffer.Length + " bytes within 8 bits.");
                case RaidoMessageSize.VariableByte:
                    headerBuffer[headerPosition++] = (byte)buffer.Length;
                    break;
                // A 16-bit size packet type.
                case RaidoMessageSize.VariableShort when buffer.Length > ushort.MaxValue:
                    throw new InvalidOperationException("Could not send a message with " + buffer.Length + " bytes within 16 bits.");
                case RaidoMessageSize.VariableShort:
                    headerBuffer[headerPosition++] = (byte)(buffer.Length >> 8);
                    headerBuffer[headerPosition++] = (byte)buffer.Length;
                    break;
                // A fixed packet type
                case RaidoMessageSize.Fixed: break;
                default: throw new NotImplementedException(nameof(writer.Size));
            }
            output.Write(headerBuffer.AsSpan()[..headerPosition]);
            buffer.CopyTo(output);
            return true;
        }
        finally
        {
            MemoryBufferWriter.Return(buffer);
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    static ClientProtocol742()
    {
        for (var i = 0; i < _payLoadSizes.Length; i++)
        {
            _payLoadSizes[i] = _defaultLength;
        }

        _payLoadSizes[48] = 4;
        _payLoadSizes[8] = 4;
        _payLoadSizes[37] = 8;
        _payLoadSizes[27] = 7;
        _payLoadSizes[24] = -2;
        _payLoadSizes[84] = -1;
        _payLoadSizes[25] = -1;
        _payLoadSizes[54] = 3;
        _payLoadSizes[36] = 3;
        _payLoadSizes[7] = 16;
        _payLoadSizes[56] = 4;
        _payLoadSizes[52] = -1;
        _payLoadSizes[91] = 9;
        _payLoadSizes[74] = 5;
        _payLoadSizes[31] = 6;
        _payLoadSizes[87] = -1;
        _payLoadSizes[64] = 0;
        _payLoadSizes[35] = 3;
        _payLoadSizes[14] = -2;
        _payLoadSizes[66] = 4;
        _payLoadSizes[20] = -1;
        _payLoadSizes[60] = 4;
        _payLoadSizes[69] = 8;
        _payLoadSizes[43] = 3;
        _payLoadSizes[88] = 3;
        _payLoadSizes[90] = 6;
        _payLoadSizes[17] = -1;
        _payLoadSizes[65] = 4;
        _payLoadSizes[21] = -1;
        _payLoadSizes[6] = 11;
        _payLoadSizes[9] = -1;
        _payLoadSizes[58] = 3;
        _payLoadSizes[29] = -2;
        _payLoadSizes[77] = -1;
        _payLoadSizes[50] = 3;
        _payLoadSizes[33] = 4;
        _payLoadSizes[45] = -1;
        _payLoadSizes[82] = 9;
        _payLoadSizes[86] = -1;
        _payLoadSizes[93] = 4;
        _payLoadSizes[53] = 3;
        _payLoadSizes[12] = -1;
        _payLoadSizes[42] = 7;
        _payLoadSizes[41] = 18;
        _payLoadSizes[57] = 0;
        _payLoadSizes[61] = 7;
        _payLoadSizes[67] = -1;
        _payLoadSizes[3] = 9;
        _payLoadSizes[55] = 3;
        _payLoadSizes[34] = 7;
        _payLoadSizes[80] = 8;
        _payLoadSizes[76] = 8;
        _payLoadSizes[89] = 15;
        _payLoadSizes[78] = 4;
        _payLoadSizes[83] = 6;
        _payLoadSizes[32] = 3;
        _payLoadSizes[81] = -1;
        _payLoadSizes[22] = 8;
        _payLoadSizes[30] = 0;
        _payLoadSizes[15] = -1;
        _payLoadSizes[38] = -1;
        _payLoadSizes[16] = 9;
        _payLoadSizes[2] = 1;
        _payLoadSizes[71] = -1;
        _payLoadSizes[10] = -2;
        _payLoadSizes[70] = 1;
        _payLoadSizes[72] = 3;
        _payLoadSizes[92] = 1;
        _payLoadSizes[68] = -1;
        _payLoadSizes[51] = 7;
        _payLoadSizes[59] = 8;
        _payLoadSizes[85] = 9;
        _payLoadSizes[26] = 16;
        _payLoadSizes[79] = 3;
        _payLoadSizes[75] = -1;
        _payLoadSizes[46] = 3;
        _payLoadSizes[11] = -2;
        _payLoadSizes[49] = 9;
        _payLoadSizes[44] = 3;
        _payLoadSizes[13] = 8;
        _payLoadSizes[19] = 4;
        _payLoadSizes[28] = 8;
        _payLoadSizes[73] = 3;
        _payLoadSizes[23] = 7;
        _payLoadSizes[1] = 1;
        _payLoadSizes[5] = 2;
        _payLoadSizes[18] = 2;
        _payLoadSizes[4] = 8;
        _payLoadSizes[63] = 2;
        _payLoadSizes[0] = 3;
        _payLoadSizes[39] = -1;
        _payLoadSizes[47] = -1;
        _payLoadSizes[62] = 7;
        _payLoadSizes[40] = 9;
        _payLoadSizes[94] = 1;
        _payLoadSizes[95] = -2;
        _payLoadSizes[96] = 15;
        _payLoadSizes[97] = -2;
        _payLoadSizes[98] = -1;
        _payLoadSizes[99] = 12;
        _payLoadSizes[100] = 0;
        _payLoadSizes[101] = -1;
        _payLoadSizes[102] = -2;
        _payLoadSizes[103] = 8;
        _payLoadSizes[104] = 3;
        _payLoadSizes[105] = 2;
        _payLoadSizes[106] = 11;
        _payLoadSizes[107] = 17;
    }
}