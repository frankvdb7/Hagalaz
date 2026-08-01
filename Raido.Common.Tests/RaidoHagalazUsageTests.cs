using System;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Raido.Common.Tests;

[TestClass]
public sealed class RaidoHagalazUsageTests
{
    [TestMethod]
    public void BinaryWriter_HagalazStylePacket_PreservesOpcodeSizeAndPayload()
    {
        using var buffer = MemoryBufferWriter.Get();
        var writer = new RaidoMessageBinaryWriter(buffer);

        writer.SetOpcode(15).SetSize(RaidoMessageSize.VariableByte);
        writer.WriteByte(3);
        writer.Write(new byte[] { 7, 8, 9 });

        Assert.AreEqual(15, writer.Opcode);
        Assert.AreEqual(RaidoMessageSize.VariableByte, writer.Size);
        CollectionAssert.AreEqual(new byte[] { 3, 7, 8, 9 }, buffer.ToArray());
    }

    [TestMethod]
    public void BinaryWriter_BitPayloadCanBeFollowedByBytePayload()
    {
        using var buffer = MemoryBufferWriter.Get();
        var writer = new RaidoMessageBinaryWriter(buffer);

        writer.BeginBitAccess().WriteBits(3, 5).EndBitAccess();
        writer.WriteByte(0x7F);

        CollectionAssert.AreEqual(new byte[] { 0xA0, 0x7F }, buffer.ToArray());
    }
}
