using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class DrawDynamicMapMessageEncoderTests
{
    private const int HeaderLength = 7;

    [TestMethod]
    public void Encode_WritesOpcode153VariableShortAndClientTransformedHeader()
    {
        var (writer, bytes) = Encode(CreateMessage(chunks: [], xteas: [], forceUpdate: true, regionPartX: 0x0191, regionPartY: 0x0192, mapSizeIndex: 2));

        Assert.AreEqual(153, writer.Opcode);
        Assert.AreEqual(RaidoMessageSize.VariableShort, writer.Size);
        // type 2 negated, Y little endian with low byte + 128, flags 128 - 1, map size, X big endian with low byte + 128
        CollectionAssert.AreEqual(new byte[] { 0xFE, 0x12, 0x01, 0x7F, 0x02, 0x01, 0x11 }, bytes);
    }

    [TestMethod]
    public void Encode_AbsentPresentZeroAndPresentChunks_WritesPresenceBitsThenDescriptors()
    {
        var chunks = new[]
        {
            DynamicMapChunk.Absent,
            DynamicMapChunk.Present(0, 0, 0, 0),
            DynamicMapChunk.Present(0x2AB, 0x4CD, 3, 2),
        };

        var (_, bytes) = Encode(CreateMessage(chunks, xteas: []));

        var reader = new BitReader(bytes, HeaderLength);
        Assert.AreEqual(0, reader.Read(1), "absent slot has no descriptor");
        Assert.AreEqual(1, reader.Read(1), "zero descriptor is present");
        Assert.AreEqual(0, reader.Read(26));
        Assert.AreEqual(1, reader.Read(1));
        var descriptor = reader.Read(26);
        Assert.AreEqual(2, descriptor >> 1 & 0x3, "rotation");
        Assert.AreEqual(0x2AB, descriptor >> 14 & 0x3ff, "source part X");
        Assert.AreEqual(0x4CD, descriptor >> 3 & 0x7ff, "source part Y");
        Assert.AreEqual(3, descriptor >> 24 & 0x3, "source plane");
        Assert.AreEqual(1 + 1 + 26 + 1 + 26, reader.BitsRead);
        Assert.AreEqual(HeaderLength + (reader.BitsRead + 7) / 8, bytes.Length);
    }

    [TestMethod]
    public void Encode_SourceRegionXteas_AreBigEndianIntsInGivenOrder()
    {
        var (_, bytes) = Encode(CreateMessage(chunks: [], xteas: [[1, 2, 3, 4], [-1, 0x01020304, 6, 7]]));

        CollectionAssert.AreEqual(
            new byte[]
            {
                0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4,
                0xFF, 0xFF, 0xFF, 0xFF, 1, 2, 3, 4, 0, 0, 0, 6, 0, 0, 0, 7
            },
            bytes[HeaderLength..]);
    }

    [TestMethod]
    public void Encode_RenderViewport_PrefixesPlayerEntryBitsBeforeDynamicBody()
    {
        var withoutPrefix = Encode(CreateMessage(chunks: [], xteas: [])).Bytes;
        var withPrefix = Encode(CreateMessage(chunks: [], xteas: [], renderViewport: true)).Bytes;

        Assert.AreEqual(withoutPrefix.Length + 4608, withPrefix.Length);
        CollectionAssert.AreEqual(withoutPrefix, withPrefix[4608..]);
        var reader = new BitReader(withPrefix, 0);
        Assert.AreEqual(3201 | 3200 << 14 | 1 << 28, reader.Read(30));
    }

    private static DrawDynamicMapMessage CreateMessage(
        IReadOnlyList<DynamicMapChunk> chunks,
        IReadOnlyList<int[]> xteas,
        bool renderViewport = false,
        bool forceUpdate = false,
        int regionPartX = 400,
        int regionPartY = 400,
        int mapSizeIndex = 0) =>
        new()
        {
            RenderViewport = renderViewport,
            ForceUpdate = forceUpdate,
            CharacterIndex = 1,
            CharacterLocation = new Location(3200, 3201, 1, 0),
            MapSizeIndex = mapSizeIndex,
            RegionPartX = regionPartX,
            RegionPartY = regionPartY,
            Chunks = chunks,
            SourceRegionXteaKeys = xteas
        };

    private static (RaidoMessageBinaryWriter Writer, byte[] Bytes) Encode(DrawDynamicMapMessage message)
    {
        using var buffer = MemoryBufferWriter.Get();
        var writer = new RaidoMessageBinaryWriter(buffer);
        new DrawDynamicMapMessageEncoder(new CharacterLocationService()).EncodeMessage(message, writer);
        return (writer, buffer.ToArray());
    }

    private sealed class BitReader(byte[] bytes, int offset)
    {
        public int BitsRead { get; private set; }

        public int Read(int count)
        {
            var value = 0;
            for (var i = 0; i < count; i++, BitsRead++)
            {
                var bit = bytes[offset + BitsRead / 8] >> (7 - BitsRead % 8) & 1;
                value = value << 1 | bit;
            }

            return value;
        }
    }
}
