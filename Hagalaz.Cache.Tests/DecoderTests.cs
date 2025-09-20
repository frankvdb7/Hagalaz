using System;
using System.IO;
using Hagalaz.Cache;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class DecoderTests
    {
        [Fact]
        public void ReferenceTableDecoder_Decode_WithInvalidProtocol_ShouldThrowInvalidDataException()
        {
            // Arrange
            var factory = new ReferenceTableCodec();
            using var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write((byte)4); // Invalid protocol
            stream.Position = 0;

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() => factory.Decode(stream, true));
            Assert.Equal("Invalid reference table protocol!", ex.Message);
        }

        // It is not currently possible to write a test that specifically triggers the null-check for a missing parent entry
        // when allocating child entries in ReferenceTableDecoder.Decode. The logic that populates the 'ids' array and
        // then creates entries for them ensures that table.GetEntry(id) will not return null in that loop.
        // The null-check was added for improved robustness and defense against future code changes, as recommended in the code review.
    }
}
