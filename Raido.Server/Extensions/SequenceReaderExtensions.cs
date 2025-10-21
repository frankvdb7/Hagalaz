using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Raido.Common;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="SequenceReader{T}"/>.
    /// </summary>
    public static class SequenceReaderExtensions
    {
        /// <summary>
        /// Tries to read a string from the sequence.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="value"></param>
        /// <param name="readStartDelimiter">Whether to read the start delimiter byte if present.</param>
        /// <returns></returns>
        public static bool TryRead(ref this SequenceReader<byte> reader, out string value, bool readStartDelimiter = false)
        {
            if (readStartDelimiter && (!reader.TryRead(out byte v) || v != RaidoConstants.StringDelimiter))
            {
                value = string.Empty;
                return false;
            }
            if (reader.TryReadTo(out ReadOnlySpan<byte> val, RaidoConstants.StringDelimiter, advancePastDelimiter: true))
            {
                value = Encoding.ASCII.GetString(val);
                return true;
            }

            value = string.Empty;
            return false;
        }

        /// <summary>
        /// Tries to read a boolean from the sequence.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="value">When this method returns, contains the boolean value, if the read operation succeeded; otherwise, the default value for the type of the <paramref name="value"/> parameter.</param>
        /// <returns><c>true</c> if the read operation succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryRead(ref this SequenceReader<byte> reader, out bool value)
        {
            if (!reader.TryRead(out byte val))
            {
                value = default;
                return false;
            }
            value = val == 1;
            return true;
        }
    }
}