using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raido.Common.Buffers;

namespace Raido.Common.Tests;

[TestClass]
public sealed class MemoryBufferWriterEdgeCaseTests
{
    [TestMethod]
    public void EmptyWriter_CopiesAsEmptyToEverySupportedDestination()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            var destination = new ArrayBufferWriter<byte>();
            var span = new byte[1];

            writer.CopyTo(destination);
            writer.CopyTo(span.AsSpan());

            Assert.IsEmpty(writer.ToArray());
            Assert.AreEqual(0, destination.WrittenCount);
            Assert.AreEqual(0, span[0]);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [TestMethod]
    public async Task ZeroLengthWrites_DoNotCreatePayload()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            writer.Write(Array.Empty<byte>(), 0, 0);
            writer.Write(ReadOnlySpan<byte>.Empty);

            using var destination = new MemoryStream();
            await writer.CopyToAsync(destination, 1, CancellationToken.None);

            Assert.AreEqual(0, writer.Length);
            Assert.IsEmpty(writer.ToArray());
            Assert.AreEqual(0, destination.Length);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [TestMethod]
    public void ByteArrayWrite_WithOffsetAndCount_CopiesOnlyRequestedRange()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            writer.Write(new byte[] { 0, 1, 2, 3, 4 }, 1, 3);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, writer.ToArray());
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    [TestMethod]
    public async Task MultiSegmentWriter_CopiesThroughBufferWriterAndSlowStreamPath()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            var first = Enumerable.Repeat((byte)1, 4096).ToArray();
            var second = new byte[] { 2, 3, 4 };
            writer.Write(first);
            writer.Write(second);

            var destination = new ArrayBufferWriter<byte>();
            writer.CopyTo(destination);
            using var stream = new MemoryStream();
            await writer.CopyToAsync(stream, 1, CancellationToken.None);

            var expected = first.Concat(second).ToArray();
            CollectionAssert.AreEqual(expected, destination.WrittenSpan.ToArray());
            CollectionAssert.AreEqual(expected, stream.ToArray());
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }
}
