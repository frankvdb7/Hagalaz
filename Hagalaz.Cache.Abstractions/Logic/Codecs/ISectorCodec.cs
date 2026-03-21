using System;
﻿using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    /// <summary>
    /// Defines the contract for a codec that handles the encoding and decoding of cache sectors.
    /// A sector is a block of data in the cache file system, typically containing a header and a data payload.
    /// </summary>
    public interface ISectorCodec
    {
        /// <summary>
        /// Decodes a byte array into a sector object.
        /// </summary>
        /// <param name="data">The raw byte data of the sector to decode.</param>
        /// <param name="extended">
        /// A flag indicating whether the sector uses an extended format. Extended formats typically
        /// support larger file IDs or additional metadata.
        /// </param>
        /// <returns>An <see cref="ISector"/> object representing the decoded data.</returns>
        ISector Decode(byte[] data, bool extended) => Decode(data.AsSpan(), extended);

        /// <summary>
        /// Decodes a span of bytes into a sector object.
        /// </summary>
        /// <param name="data">The raw byte data of the sector to decode.</param>
        /// <param name="extended">
        /// A flag indicating whether the sector uses an extended format.
        /// </param>
        /// <returns>An <see cref="ISector"/> object representing the decoded data.</returns>
        ISector Decode(ReadOnlySpan<byte> data, bool extended);

        /// <summary>
        /// Encodes a sector object into a byte array.
        /// </summary>
        /// <param name="sector">The sector object to encode, containing header information.</param>
        /// <param name="dataBlock">The raw data payload that belongs to the sector.</param>
        /// <returns>A byte array representing the fully encoded sector, including its header and data block.</returns>
        byte[] Encode(ISector sector, byte[] dataBlock) => Encode(sector, dataBlock.AsSpan());

        /// <summary>
        /// Encodes a sector object into a byte array.
        /// </summary>
        /// <param name="sector">The sector object to encode, containing header information.</param>
        /// <param name="dataBlock">The raw data payload that belongs to the sector.</param>
        /// <returns>A byte array representing the fully encoded sector, including its header and data block.</returns>
        byte[] Encode(ISector sector, ReadOnlySpan<byte> dataBlock);

        /// <summary>
        /// Encodes a sector object into a span of bytes.
        /// </summary>
        /// <param name="sector">The sector object to encode.</param>
        /// <param name="dataBlock">The raw data payload.</param>
        /// <param name="destination">The destination span where the encoded sector will be written.</param>
        void Encode(ISector sector, ReadOnlySpan<byte> dataBlock, Span<byte> destination);
    }
}